using bankrupt_piterjust.Helpers;
using bankrupt_piterjust.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;

namespace bankrupt_piterjust.Services
{
    public class DocumentGenerationService
    {
        public async Task<string?> GenerateContractAsync(int debtorId)
        {
            try
            {
                var debtorRepository = new DebtorRepository();

                // Get person information
                var person = await debtorRepository.GetPersonByIdAsync(debtorId);
                if (person == null)
                    throw new Exception("Информация о должнике не найдена.");

                // Get passport information
                var passport = await debtorRepository.GetPassportByPersonIdAsync(debtorId);
                if (passport == null)
                    throw new Exception("Паспортные данные должника не найдены.");

                // Get address information
                var addresses = await debtorRepository.GetAddressesByPersonIdAsync(debtorId);
                var registrationAddress = addresses.FirstOrDefault(a => a.AddressType == AddressType.Registration);
                if (registrationAddress == null)
                    throw new Exception("Адрес регистрации должника не найден.");

                // Get representative (authorized employee) information
                var representative = UserSessionService.Instance.CurrentEmployee;
                if (representative == null)
                    throw new Exception("Информация об авторизованном сотруднике не найдена.");

                // Get contract template path
                string templatePath = FindTemplateFile();
                if (string.IsNullOrEmpty(templatePath))
                    throw new Exception("Шаблон договора не найден по пути: Documents/Договор_Юридических_Услуг.docx");

                // Create Generated directory if it doesn't exist
                var documentsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Generated");
                if (!Directory.Exists(documentsDir))
                {
                    Directory.CreateDirectory(documentsDir);
                }

                // Default filename for the generated document
                string defaultFileName = $"Договор_{person.LastName}_{DateTime.Now:dd.MM.yyyy}.docx";

                // Show save dialog to select output location
                var saveDialog = new SaveFileDialog
                {
                    InitialDirectory = documentsDir,
                    Filter = "Документ Word (*.docx)|*.docx",
                    Title = "Сохранить договор",
                    FileName = defaultFileName
                };

                if (saveDialog.ShowDialog() != true)
                    return null;

                string outputPath = saveDialog.FileName;

                // Create a copy of the template
                File.Copy(templatePath, outputPath, true);

                // Generate contract number based on person ID and current date
                string contractNumber = $"БФЛ-{debtorId}-{DateTime.Now:ddMMyy}";

                // Set contract cost and convert to words
                decimal contractTotalCost = 100000m;
                decimal mandatoryExpenses = 25000m;
                decimal managerFee = 25000m;
                decimal otherExpenses = 50000m;

                // Use NumberToWordsConverter for Russian words
                string contractTotalCostWords = NumberToWordsConverter.ConvertToWords(contractTotalCost);
                string mandatoryExpensesWords = NumberToWordsConverter.ConvertToWords(mandatoryExpenses);

                // Representative information
                string representativeName = representative.FullName;
                string representativePosition = representative.Position;
                string representativeBasis = $"Доверенность № {new Random().Next(100, 999)} от {DateTime.Now.AddMonths(-1):dd.MM.yyyy}";

                // Prepare replacement data
                var replacements = new Dictionary<string, string>
                {
                    { "<номер_договора>", contractNumber },
                    { "<город_составления>", "Санкт-Петербург" },
                    { "<дата_составления>", DateTime.Now.ToString("dd.MM.yyyy") },
                    { "<фио_заказчика>", person.FullName },
                    { "<серия_паспорта>", passport.Series },
                    { "<номер_паспорта>", passport.Number },
                    { "<кем_выдан_паспорт>", passport.IssuedBy },
                    { "<код_подразделения>", passport.DivisionCode ?? "-" }, // Use null-coalescing operator to provide a default value
                    { "<дата_выдачи>", passport.IssueDate.ToString("dd.MM.yyyy") },
                    { "<адрес_регистрации_заказчика>", registrationAddress.AddressText },
                    { "<фио_представителя_исполнителя>", representativeName },
                    { "<должность_представителя>", representativePosition },
                    { "<основание_действий_представителя>", representativeBasis },
                    { "<cтоимость_договора>", contractTotalCost.ToString("#,##0.00") },
                    { "<стоимость_договора_прописью>", contractTotalCostWords },
                    { "<сумма_обязательных_расходов>", mandatoryExpenses.ToString("#,##0.00") },
                    { "<сумма_обязательных_расходов_прописью>", mandatoryExpensesWords },
                    { "<размер_вознаграждения_фин_управляющего>", managerFee.ToString("#,##0.00") },
                    { "<прочие_расходы_банкротства>", otherExpenses.ToString("#,##0.00") },
                    { "<номер_телефона_заказчика>", person.Phone ?? "-" }, // Use null-coalescing operator to provide a default value
                    { "<электронный_адрес_заказчика>", person.Email ?? "-" } // Use null-coalescing operator to provide a default value
                };

                // Replace tags in document
                using (WordprocessingDocument document = WordprocessingDocument.Open(outputPath, true))
                {
                    int totalReplacements = 0;

                    foreach (var replacement in replacements)
                    {
                        int tagsReplaced = ReplaceTextInDocument(document, replacement.Key, replacement.Value);
                        totalReplacements += tagsReplaced;
                    }

                    if (totalReplacements == 0)
                    {
                        // If no tags were found, the template might not be set up correctly
                        MessageBox.Show(
                            "В шаблоне договора не найдены теги для замены. Проверьте, что шаблон содержит необходимые теги для заполнения.",
                            "Предупреждение",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }

                return outputPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации договора: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// Searches for the contract template file in multiple possible locations
        /// </summary>
        /// <returns>The path to the template file, or null if not found</returns>
        private string FindTemplateFile()
        {
            string templateName = "Договор_Юридических_Услуг.docx";

            // First check in the application directory
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents", templateName);
            if (File.Exists(templatePath))
                return templatePath;

            // Try looking in the solution directory structure
            string solutionDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] possiblePaths = new string[]
            {
                Path.Combine(solutionDir, "Documents", templateName),
                Path.Combine(solutionDir, "..", "Documents", templateName),
                Path.Combine(solutionDir, "..", "..", "Documents", templateName),
                Path.Combine(solutionDir, "..", "..", "..", "Documents", templateName)
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>
        /// Replaces all occurrences of a specific text tag in a Word document with a replacement value.
        /// Handles cases where tags might be split across multiple text runs.
        /// </summary>
        /// <returns>The number of tag occurrences that were replaced</returns>
        private int ReplaceTextInDocument(WordprocessingDocument document, string searchText, string replaceText)
        {
            int totalReplacements = 0;

            // Process the main document body
            if (document.MainDocumentPart?.Document?.Body != null)
            {
                totalReplacements += ProcessTextElements(document.MainDocumentPart.Document.Body, searchText, replaceText);
            }

            // Process headers
            foreach (var headerPart in document.MainDocumentPart.HeaderParts)
            {
                totalReplacements += ProcessTextElements(headerPart.Header, searchText, replaceText);
            }

            // Process footers
            foreach (var footerPart in document.MainDocumentPart.FooterParts)
            {
                totalReplacements += ProcessTextElements(footerPart.Footer, searchText, replaceText);
            }

            // Process any other document parts that might contain text
            if (document.MainDocumentPart.FootnotesPart != null)
            {
                totalReplacements += ProcessTextElements(document.MainDocumentPart.FootnotesPart.Footnotes, searchText, replaceText);
            }

            if (document.MainDocumentPart.EndnotesPart != null)
            {
                totalReplacements += ProcessTextElements(document.MainDocumentPart.EndnotesPart.Endnotes, searchText, replaceText);
            }

            return totalReplacements;
        }

        /// <summary>
        /// Processes all text elements in a given element and replaces all occurrences of the search text
        /// </summary>
        /// <returns>The number of replacements made</returns>
        private int ProcessTextElements<T>(T element, string searchText, string replaceText) where T : OpenXmlElement
        {
            int totalReplacements = 0;

            // First attempt: Replace within each Text element
            foreach (var text in element.Descendants<Text>())
            {
                if (text.Text.Contains(searchText))
                {
                    // Replace all occurrences within this text element
                    string originalText = text.Text;
                    string newText = originalText.Replace(searchText, replaceText);

                    // Count how many replacements were made
                    int replacementsInThisElement = 0;
                    
                    // If search and replace text have different lengths, calculate based on length difference
                    if (searchText.Length != replaceText.Length)
                    {
                        int lengthDifference = searchText.Length - replaceText.Length;
                        if (lengthDifference > 0)
                        {
                            // Search text is longer than replace text
                            replacementsInThisElement = (originalText.Length - newText.Length) / lengthDifference;
                        }
                        else
                        {
                            // Replace text is longer than search text
                            replacementsInThisElement = (newText.Length - originalText.Length) / Math.Abs(lengthDifference);
                        }
                    }
                    else
                    {
                        // If lengths are equal, count occurrences manually
                        int searchIndex = 0;
                        while ((searchIndex = originalText.IndexOf(searchText, searchIndex)) != -1)
                        {
                            replacementsInThisElement++;
                            searchIndex += searchText.Length;
                        }
                    }

                    text.Text = newText;
                    totalReplacements += replacementsInThisElement;
                }
            }

            // Handle tags that might be split across multiple runs
            totalReplacements += HandleSplitTags(element, searchText, replaceText);

            return totalReplacements;
        }

        /// <summary>
        /// Handles tags that might be split across multiple runs in the document
        /// </summary>
        /// <returns>The number of split tags that were replaced</returns>
        private int HandleSplitTags<T>(T element, string searchText, string replaceText) where T : OpenXmlElement
        {
            int totalReplacements = 0;

            // Process each paragraph
            foreach (var paragraph in element.Descendants<Paragraph>())
            {
                // Get all text runs in the paragraph
                var runs = paragraph.Descendants<Run>().ToList();
                if (runs.Count < 2) continue; // Need at least 2 runs for a split tag

                // Build a string with all text content to check if our tag exists across multiple runs
                var paragraphText = new StringBuilder();
                foreach (var run in runs)
                {
                    foreach (var text in run.Descendants<Text>())
                    {
                        paragraphText.Append(text.Text);
                    }
                }

                // Check if the tag exists in the combined text
                string fullText = paragraphText.ToString();
                int tagIndex = 0;

                // Loop through all occurrences of the tag in the paragraph
                while ((tagIndex = fullText.IndexOf(searchText, tagIndex)) != -1)
                {
                    int tagStartIndex = tagIndex;
                    int tagEndIndex = tagStartIndex + searchText.Length - 1;

                    // Track our position in the paragraph text
                    int currentPosition = 0;

                    // A list to store runs that contain the tag or parts of it
                    var runsToModify = new List<RunInfo>();

                    // Identify runs that contain parts of the tag
                    foreach (var run in runs)
                    {
                        foreach (var text in run.Descendants<Text>())
                        {
                            int textLength = text.Text.Length;
                            int textStartIndex = currentPosition;
                            int textEndIndex = currentPosition + textLength - 1;

                            // Check if this text element overlaps with the tag
                            if (!(textEndIndex < tagStartIndex || textStartIndex > tagEndIndex))
                            {
                                // This text element contains part of the tag
                                runsToModify.Add(new RunInfo
                                {
                                    Run = run,
                                    Text = text,
                                    TextStartIndex = Math.Max(0, tagStartIndex - textStartIndex),
                                    TextEndIndex = Math.Min(textLength - 1, tagEndIndex - textStartIndex)
                                });
                            }

                            currentPosition += textLength;
                        }
                    }

                    // If we found runs to modify
                    if (runsToModify.Count > 0)
                    {
                        // Special case: If the tag is fully within a single Text element
                        if (runsToModify.Count == 1 &&
                            runsToModify[0].TextStartIndex == 0 &&
                            runsToModify[0].TextEndIndex == runsToModify[0].Text.Text.Length - 1)
                        {
                            runsToModify[0].Text.Text = replaceText;
                        }
                        else
                        {
                            // Handle the more complex case where the tag spans multiple runs
                            // We'll replace the tag in the first run and remove tag parts from other runs
                            for (int i = 0; i < runsToModify.Count; i++)
                            {
                                var runInfo = runsToModify[i];

                                if (i == 0) // First run containing part of the tag
                                {
                                    // Replace from start index to the end of the text with the new value
                                    string beforeTag = runInfo.Text.Text.Substring(0, runInfo.TextStartIndex);
                                    runInfo.Text.Text = beforeTag + replaceText;
                                }
                                else if (i == runsToModify.Count - 1) // Last run containing part of the tag
                                {
                                    // Keep only the text after the tag
                                    if (runInfo.TextEndIndex < runInfo.Text.Text.Length - 1)
                                    {
                                        runInfo.Text.Text = runInfo.Text.Text.Substring(runInfo.TextEndIndex + 1);
                                    }
                                    else
                                    {
                                        // If the tag ends at the end of the text, clear the text
                                        runInfo.Text.Text = string.Empty;
                                    }

                                    // If the text is now empty, remove the run (but only if it's not the only run left)
                                    if (string.IsNullOrEmpty(runInfo.Text.Text) && runs.Count > 1)
                                    {
                                        runInfo.Run.Remove();
                                    }
                                }
                                else // Middle runs containing part of the tag
                                {
                                    // Remove these runs entirely as their content is part of the tag
                                    // (but only if it's not the only run left)
                                    if (runs.Count > 1)
                                    {
                                        runInfo.Run.Remove();
                                    }
                                    else
                                    {
                                        runInfo.Text.Text = string.Empty;
                                    }
                                }
                            }
                        }

                        totalReplacements++;

                        // Move past this occurrence to find the next one
                        tagIndex = tagStartIndex + replaceText.Length;

                        // Need to rebuild runs list as we've modified the structure
                        runs = paragraph.Descendants<Run>().ToList();

                        // Rebuild the paragraph text for the next iteration
                        paragraphText.Clear();
                        foreach (var run in runs)
                        {
                            foreach (var text in run.Descendants<Text>())
                            {
                                paragraphText.Append(text.Text);
                            }
                        }
                        fullText = paragraphText.ToString();
                    }
                    else
                    {
                        // If we couldn't find the runs to modify, just move past this occurrence
                        tagIndex = tagStartIndex + 1;
                    }
                }
            }

            return totalReplacements;
        }

        /// <summary>
        /// Helper class to store information about a run and its text element
        /// </summary>
        private class RunInfo
        {
            public Run Run { get; set; }
            public Text Text { get; set; }
            public int TextStartIndex { get; set; }
            public int TextEndIndex { get; set; }
        }
    }
}
