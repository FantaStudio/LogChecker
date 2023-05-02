using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using LogChecker;
using Ookii.Dialogs.Wpf;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LogAnalyzer.ViewModels;
using MessageBox = System.Windows.MessageBox;
using Style = DocumentFormat.OpenXml.Wordprocessing.Style;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using ParagraphProperties = DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace LogAnalyzer
{
    public partial class MainWindow : Window
    {
        private readonly VistaFolderBrowserDialog _folderDialog = new();

        private readonly VistaFolderBrowserDialog _reportDialog = new()
        {
            Description = "Выберите папку, куда сохранить отчёт"
        };

        private static readonly JsonSerializerOptions EncoderSettings = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };

        private string[]? _logFiles;

        private MainWindowViewModel Model { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Model = new MainWindowViewModel();
            DataContext = Model;
        }

        private void ChooseFolder_OnClick(object sender, RoutedEventArgs e)
        {
            if (_folderDialog.ShowDialog() != true)
                return;

            var files = Directory.GetFiles(_folderDialog.SelectedPath);
            if (files.Length < 1)
            {
                MessageBox.Show("Ошибка", "В выбранной папке нет файлов!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _logFiles = files;

            Model.FolderPath = _folderDialog.SelectedPath;
            StatusBar.Visibility = Visibility.Collapsed;
        }

        private async void MakeReport_OnClick(object sender, RoutedEventArgs e)
        {
            if (_reportDialog.ShowDialog() != true)
                return;

            StatusBar.Visibility = Visibility.Visible;

            var exceptions = await AnalyzeLogFiles();

            var reportDate = DateTime.Now.ToString("MM.dd.yyyy HH_mm_ss");

            CreateReportDocument($"{_reportDialog.SelectedPath}/logReport-{reportDate}.doc", exceptions);
            Model.Progress = 100;

            MessageBox.Show("Отчёт успешно сформирован", "Крутяк:)", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task<Dictionary<string, LogElementsCounter>> AnalyzeLogFiles()
        {
            Dictionary<string, LogElementsCounter> exceptions = new();
            Model.Errors = 0;
            Model.Successes = 0;
            Model.Progress = 0;

            var perFileProgress = 90 / _logFiles!.Length;

            foreach (var path in _logFiles!)
            {

                using var reader = new StreamReader(path);

                string? line = null;
                do
                {
                    try
                    {
                        line = await reader.ReadLineAsync();
                        if (line == null)
                            continue;

                        var log = JsonSerializer.Deserialize<LogElement>(line, EncoderSettings);
                        if (log?.Exception == null) continue;
                        if (Model.IgnoreTimeout && log.Exception.Contains("System.TimeoutException")) continue;

                        if (!exceptions.ContainsKey(log.Exception))
                        {
                            exceptions.Add(log.Exception, new LogElementsCounter { LogElement = log });
                        }
                        else
                        {
                            exceptions[log.Exception].Count++;
                        }

                        Model.Successes++;
                    }
                    catch (Exception)
                    {
                        Model.Errors++;
                    }
                } 
                while (line != null);

                Model.Progress += perFileProgress;
            }

            return exceptions
                .Where(x => x.Value.Count >= Model.MinCount)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// Создание отчёта в Word
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="exceptions"></param>
        private static void CreateReportDocument(string filePath, Dictionary<string, LogElementsCounter> exceptions)
        {
            var exceptionGroups = exceptions.GroupBy(x => x.Value.LogElement.Properties.Application);

            using var wordDocument =
                WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);

            // Добавляем основную часть документа
            var mainPart = wordDocument.AddMainDocumentPart();

            // Создаем новый документ
            mainPart.Document = new Document();

            // Добавляем стиль для заголовков
            var stylePart = mainPart.AddNewPart<StyleDefinitionsPart>();
            stylePart.Styles = new Styles();
            stylePart.Styles.Append(new Style
            {
                Type = StyleValues.Paragraph,
                StyleId = "Heading1",
                StyleName = new StyleName { Val = "Heading 1" },
                BasedOn = new BasedOn { Val = "Normal" },
                NextParagraphStyle = new NextParagraphStyle { Val = "Normal" },
                Rsid = new Rsid { Val = "00F732D4" },
                StyleRunProperties = new StyleRunProperties { 
                    Bold = new Bold(), 
                    FontSize = new FontSize
                    {
                        Val = "32"
                    }
                },
                StyleParagraphProperties = new StyleParagraphProperties
                {
                    SpacingBetweenLines = new SpacingBetweenLines 
                    {
                        Before = "240",
                        After = "0"
                    }
                }
            });

            var bodyContent = new List<OpenXmlElement>();
            foreach (var exceptionGroup in exceptionGroups)
            {
                // Создаем новый параграф со стилем "Заголовок"
                var heading = new Paragraph(
                    new ParagraphProperties(
                        new ParagraphStyleId { Val = "Heading1" }
                    ),
                    new Run(
                        new Text(exceptionGroup.Key)
                    )
                );
                bodyContent.Add(heading);

                var orderedLog = exceptionGroup.OrderByDescending(x => x.Value.Count);
                foreach (var log in orderedLog)
                {
                    // Создаем новый параграф с текстом кол-ва
                    var countText = new Paragraph(
                        new Run(
                            new Text(log.Value.Count.ToString("Кол-во: 0")),
                            new Break()
                        )
                    );

                    // Убираем отсутпы после параграфа
                    countText.ParagraphProperties = new ParagraphProperties
                    {
                        SpacingBetweenLines = new SpacingBetweenLines
                        {
                            After = "0"
                        }
                    };


                    // Создаем новый параграф с текстом ошибки
                    var errorText = new Paragraph();

                    var logJson = JsonSerializer.Serialize(log.Value.LogElement, EncoderSettings);
                    var lines = logJson.Split('\n');

                    // Добавляем каждую строку в документ, с отступом перед каждой строкой
                    foreach (var line in lines)
                    {
                        var wordLine = new Run(new Text(line.TrimEnd('\r'))
                        {
                            Space = SpaceProcessingModeValues.Preserve
                        });

                        errorText.Append(wordLine, new Break());
                    }

                    bodyContent.Add(countText);
                    bodyContent.Add(errorText);
                }
            }

            // Добавляем содержимое в документ
            mainPart.Document.Append(new Body(bodyContent));

            // Создаём элемент оглавления
            var sdtBlock = new SdtBlock
            {
                InnerXml = GetToc("Оглавление", 16)
            };

            // Добавляем оглавление в начало документа
            mainPart.Document.InsertAt(sdtBlock, 0);

            // Создаём класс настроек
            var settingsPart = mainPart.AddNewPart<DocumentSettingsPart>();
            settingsPart.Settings = new Settings
            {
                //отключаем границы страницы вокруг футера
                BordersDoNotSurroundFooter = new BordersDoNotSurroundFooter
                {
                    Val = true
                },

                // Скрываем грамматические ошибки
                HideGrammaticalErrors = new HideGrammaticalErrors
                {
                    Val = true
                },

                // Скрываем ошибки написания
                HideSpellingErrors = new HideSpellingErrors
                {
                    Val = true
                }
            };

            var updateOnOpenSettings = new UpdateFieldsOnOpen
            {
                Val = true
            };

            settingsPart.Settings.Append(updateOnOpenSettings);
            settingsPart.Settings.Save();
        }

        /// <summary>
        /// Формирует пустое оглавление
        /// </summary>
        /// <param name="title">Заголовок оглавления</param>
        /// <param name="titleFontSize">Размер шрифта заголовка</param>
        /// <returns></returns>
        private static string GetToc(string title, int titleFontSize)
        {
            return $@"<w:sdt>
             <w:sdtPr>
                <w:id w:val=""-493258456"" />
                <w:docPartObj>
                   <w:docPartGallery w:val=""Table of Contents"" />
                   <w:docPartUnique />
                </w:docPartObj>
             </w:sdtPr>
             <w:sdtEndPr>
                <w:rPr>
                   <w:rFonts w:asciiTheme=""minorHAnsi"" w:eastAsiaTheme=""minorHAnsi"" w:hAnsiTheme=""minorHAnsi"" w:cstheme=""minorBidi"" />
                   <w:b />
                   <w:bCs />
                   <w:noProof />
                   <w:color w:val=""auto"" />
                   <w:sz w:val=""22"" />
                   <w:szCs w:val=""22"" />
                </w:rPr>
             </w:sdtEndPr>
             <w:sdtContent>
                <w:p w:rsidR=""00095C65"" w:rsidRDefault=""00095C65"">
                   <w:pPr>
                      <w:pStyle w:val=""TOCHeading"" />
                      <w:jc w:val=""center"" /> 
                   </w:pPr>
                   <w:r>
                        <w:rPr>
                          <w:b /> 
                          <w:color w:val=""2E74B5"" w:themeColor=""accent1"" w:themeShade=""BF"" /> 
                          <w:sz w:val=""{titleFontSize * 2}"" /> 
                          <w:szCs w:val=""{titleFontSize * 2}"" /> 
                      </w:rPr>
                      <w:t>{title}</w:t>
                   </w:r>
                </w:p>
                <w:p w:rsidR=""00095C65"" w:rsidRDefault=""00095C65"">
                   <w:r>
                      <w:rPr>
                         <w:b />
                         <w:bCs />
                         <w:noProof />
                      </w:rPr>
                      <w:fldChar w:fldCharType=""begin"" />
                   </w:r>
                   <w:r>
                      <w:rPr>
                         <w:b />
                         <w:bCs />
                         <w:noProof />
                      </w:rPr>
                      <w:instrText xml:space=""preserve""> TOC \o ""1-3"" \h \z \u </w:instrText>
                   </w:r>
                   <w:r>
                      <w:rPr>
                         <w:b />
                         <w:bCs />
                         <w:noProof />
                      </w:rPr>
                      <w:fldChar w:fldCharType=""separate"" />
                   </w:r>
                   <w:r>
                      <w:rPr>
                         <w:noProof />
                      </w:rPr>
                      <w:t>No table of contents entries found.</w:t>
                   </w:r>
                   <w:r>
                      <w:rPr>
                         <w:b />
                         <w:bCs />
                         <w:noProof />
                      </w:rPr>
                      <w:fldChar w:fldCharType=""end"" />
                   </w:r>
                </w:p>
             </w:sdtContent>
          </w:sdt>";
        }

        private void CountIgnoreTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
            {
                e.Handled = true;
            }
        }
    }
}
