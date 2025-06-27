using bankrupt_piterjust.Helpers;
using bankrupt_piterjust.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Win32;
using System.Globalization;
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
                var registrationAddress = addresses.FirstOrDefault();
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

                File.Copy(templatePath, outputPath, true);

                var repo = new FullDatabaseRepository();
                int debtorIdFromPersonId = await repo.GetDebtorIdByPersonIdAsync(debtorId);

                var contractInfo = await repo.GetLatestContractByDebtorIdAsync(debtorIdFromPersonId);
                if (contractInfo == null)
                    throw new Exception("Договор для должника не найден.");

                string contractNumber = $"{contractInfo.ContractNumber}-БФЛ{DateTime.Now:yy}";

                decimal contractTotalCost = contractInfo.TotalCost;
                decimal mandatoryExpenses = contractInfo.MandatoryExpenses;
                decimal managerFee = contractInfo.ManagerFee;
                decimal servicesAmount = contractInfo.TotalCost - contractInfo.MandatoryExpenses;
                decimal otherExpenses = contractInfo.OtherExpenses;

                var paymentSchedule = await repo.GetPaymentScheduleByContractIdAsync(contractInfo.ContractId);
                string representativeName = representative.FullName;
                string representativePosition = representative.Position;
                string? representativeBasis = await repo.GetEmployeeBasisStringAsync(representative.EmployeeId);
                var replacements = new Dictionary<string, string>
                {
                    { "<номер_договора>", contractNumber },
                    { "<город_составления>", contractInfo.City },
                    { "<дата_составления>", contractInfo.ContractDate.ToString("dd.MM.yyyy") },
                    { "<фио_заказчика>", person.FullName },
                    { "<серия_паспорта>", passport.Series },
                    { "<номер_паспорта>", passport.Number },
                    { "<кем_выдан_паспорт>", passport.IssuedBy },
                    { "<код_подразделения>", passport.DivisionCode ?? "-" }, // Use null-coalescing operator to provide a default value
                    { "<дата_выдачи>", passport.IssueDate.ToString("dd.MM.yyyy") },
                    { "<адрес_регистрации_заказчика>", FormatAddress(registrationAddress) },
                    { "<фио_представителя_исполнителя>", representativeName },
                    { "<должность_представителя>", representativePosition },
                    { "<основание_действий_представителя>", representativeBasis },
                    { "<cтоимость_юридических_услуг>", servicesAmount.ToString("#,##0.00") }, // Changed from contractTotalCost to managerFee
                    { "<cтоимость_юридических_услуг_прописью>", NumberToWordsConverter.ConvertToWords(servicesAmount) }, // Changed from contractTotalCostWords to direct conversion
                    { "<сумма_обязательных_расходов>", mandatoryExpenses.ToString("#,##0.00") },
                    { "<сумма_обязательных_расходов_прописью>", NumberToWordsConverter.ConvertToWords(mandatoryExpenses) },
                    { "<размер_вознаграждения_фин_управляющего>", managerFee.ToString("#,##0.00") },
                    { "<прочие_расходы_банкротства>", otherExpenses.ToString("#,##0.00") },
                    { "<стоимость_первого_этапа>", contractInfo.Stage1Cost.ToString("#,##0.00") },
                    { "<стоимость_первого_этапа_прописью>", NumberToWordsConverter.ConvertToWords(contractInfo.Stage1Cost) },
                    { "<стоимость_второго_этапа>", contractInfo.Stage2Cost.ToString("#,##0.00") },
                    { "<стоимость_второго_этапа_прописью>", NumberToWordsConverter.ConvertToWords(contractInfo.Stage2Cost) },
                    { "<стоимость_третьего_этапа>", contractInfo.Stage3Cost.ToString("#,##0.00") },
                    { "<стоимость_третьего_этапа_прописью>", NumberToWordsConverter.ConvertToWords(contractInfo.Stage3Cost) },
                    { "<номер_телефона_заказчика>", person.Phone ?? "-" }, // Use null-coalescing operator to provide a default value
                    { "<электронный_адрес_заказчика>", person.Email ?? "-" } // Use null-coalescing operator to provide a default value
                };

                // Add gender-based ending tag
                string genderEnding = person.IsMale ? "ый" : "ая";
                replacements.Add("<окончание_пола>", genderEnding);

                // Replace tags in document
                using (WordprocessingDocument document = WordprocessingDocument.Open(outputPath, true))
                {
                    int totalReplacements = 0;

                    foreach (var replacement in replacements)
                    {
                        int tagsReplaced = ReplaceTextInDocument(document, replacement.Key, replacement.Value);
                        totalReplacements += tagsReplaced;
                    }

                    // Insert payment schedule into table if available
                    if (paymentSchedule.Any())
                    {
                        FillPaymentScheduleTable(document, paymentSchedule);
                    }

                    if (totalReplacements == 0)
                    {
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
        private string FindTemplateFile()
        {
            string templateName = "Договор_Юридических_Услуг.docx";

            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TemplatesDocx", templateName);
            if (File.Exists(templatePath))
                return templatePath;

            string solutionDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] possiblePaths = new string[]
            {
                Path.Combine(solutionDir, "TemplatesDocx", templateName),
                Path.Combine(solutionDir, "..", "TemplatesDocx", templateName),
                Path.Combine(solutionDir, "..", "..", "TemplatesDocx", templateName),
                Path.Combine(solutionDir, "..", "..", "..", "TemplatesDocx", templateName)
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null!;
        }

        private int ReplaceTextInDocument(WordprocessingDocument document, string searchText, string replaceText)
        {
            int totalReplacements = 0;

            if (document.MainDocumentPart?.Document?.Body != null)
            {
                totalReplacements += ProcessTextElements(document.MainDocumentPart.Document.Body, searchText, replaceText);
            }

            foreach (var headerPart in document.MainDocumentPart.HeaderParts)
            {
                totalReplacements += ProcessTextElements(headerPart.Header, searchText, replaceText);
            }

            foreach (var footerPart in document.MainDocumentPart.FooterParts)
            {
                totalReplacements += ProcessTextElements(footerPart.Footer, searchText, replaceText);
            }

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

        private int ProcessTextElements<T>(T element, string searchText, string replaceText) where T : OpenXmlElement
        {
            int totalReplacements = 0;

            foreach (var text in element.Descendants<Text>())
            {
                if (text.Text.Contains(searchText))
                {
                    string originalText = text.Text;
                    string newText = originalText.Replace(searchText, replaceText);
                    int replacementsInThisElement = 0;

                    if (searchText.Length != replaceText.Length)
                    {
                        int lengthDifference = searchText.Length - replaceText.Length;
                        if (lengthDifference > 0)
                        {
                            replacementsInThisElement = (originalText.Length - newText.Length) / lengthDifference;
                        }
                        else
                        {
                            replacementsInThisElement = (newText.Length - originalText.Length) / Math.Abs(lengthDifference);
                        }
                    }
                    else
                    {
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

            totalReplacements += HandleSplitTags(element, searchText, replaceText);

            return totalReplacements;
        }

        private int HandleSplitTags<T>(T element, string searchText, string replaceText) where T : OpenXmlElement
        {
            int totalReplacements = 0;

            foreach (var paragraph in element.Descendants<Paragraph>())
            {
                var runs = paragraph.Descendants<Run>().ToList();
                if (runs.Count < 2) continue; 

                var paragraphText = new StringBuilder();
                foreach (var run in runs)
                {
                    foreach (var text in run.Descendants<Text>())
                    {
                        paragraphText.Append(text.Text);
                    }
                }

                string fullText = paragraphText.ToString();
                int tagIndex = 0;


                while ((tagIndex = fullText.IndexOf(searchText, tagIndex)) != -1)
                {
                    int tagStartIndex = tagIndex;
                    int tagEndIndex = tagStartIndex + searchText.Length - 1;

                    int currentPosition = 0;

                    var runsToModify = new List<RunInfo>();

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

        /// <summary>
        /// Inserts payment schedule rows into the contract document table.
        /// Looks for a row containing the payment tags and duplicates it for
        /// each payment in the schedule.
        /// </summary>
        private void FillPaymentScheduleTable(WordprocessingDocument document, IEnumerable<PaymentSchedule> payments)
        {
            var body = document.MainDocumentPart.Document.Body;
            if (body == null)
                return;

            var templateRow = body.Descendants<TableRow>()
                .FirstOrDefault(r => r.InnerText.Contains("<номер_платежа>") || r.InnerText.Contains("<платеж_до_даты>"));
            if (templateRow == null)
                return;

            var table = templateRow.Ancestors<Table>().FirstOrDefault();
            if (table == null)
                return;

            TableRow lastRow = templateRow;
            foreach (var p in payments)
            {
                var newRow = (TableRow)templateRow.CloneNode(true);
                var replacements = new Dictionary<string, string>
                {
                    {"<номер_платежа>", p.Stage.ToString()},
                    {"<платеж_до_даты>", FormatDueDate(p.DueDate)},
                    {"<наименование_работ>", p.Description},
                    {"<сумма_платежа>", p.Amount.ToString("#,##0.00")}
                };

                foreach (var rep in replacements)
                {
                    ProcessTextElements(newRow, rep.Key, rep.Value);
                }

                table.InsertAfter(newRow, lastRow);
                lastRow = newRow;
            }

            templateRow.Remove();
        }

        /// <summary>
        /// Formats a due date as "до 12 января 2025 года".
        /// </summary>
        private string FormatDueDate(DateTime? date)
        {
            if (!date.HasValue)
                return string.Empty;

            var culture = new CultureInfo("ru-RU");
            string formatted = date.Value.ToString("d MMMM yyyy", culture);
            return $"до {formatted} года";
        }

        private static string FormatAddress(Address address)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(address.PostalCode)) parts.Add(address.PostalCode);
            if (!string.IsNullOrWhiteSpace(address.Country)) parts.Add(address.Country);
            if (!string.IsNullOrWhiteSpace(address.Region)) parts.Add(address.Region);
            if (!string.IsNullOrWhiteSpace(address.District)) parts.Add("район " + address.District);
            if (!string.IsNullOrWhiteSpace(address.City)) parts.Add(address.City);
            if (!string.IsNullOrWhiteSpace(address.Locality)) parts.Add(address.Locality);
            if (!string.IsNullOrWhiteSpace(address.Street)) parts.Add(address.Street);
            if (!string.IsNullOrWhiteSpace(address.HouseNumber)) parts.Add("д." + address.HouseNumber);
            if (!string.IsNullOrWhiteSpace(address.Building)) parts.Add("к." + address.Building);
            if (!string.IsNullOrWhiteSpace(address.Apartment)) parts.Add("кв." + address.Apartment);
            return string.Join(", ", parts);
        }
    }
}
