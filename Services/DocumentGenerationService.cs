using System;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using bankrupt_piterjust.Models;
using Microsoft.Win32;
using bankrupt_piterjust.Helpers;

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

                // Get contract template path
                string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents", "Договор_Юридических_Услуг.docx");
                if (!File.Exists(templatePath))
                {
                    // Try looking in the solution directory structure
                    string solutionDir = AppDomain.CurrentDomain.BaseDirectory;
                    string[] possiblePaths = new string[]
                    {
                        Path.Combine(solutionDir, "Documents", "Договор_Юридических_Услуг.docx"),
                        Path.Combine(solutionDir, "..", "Documents", "Договор_Юридических_Услуг.docx"),
                        Path.Combine(solutionDir, "..", "..", "Documents", "Договор_Юридических_Услуг.docx"),
                        Path.Combine(solutionDir, "..", "..", "..", "Documents", "Договор_Юридических_Услуг.docx")
                    };
                    
                    foreach (var path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            templatePath = path;
                            break;
                        }
                    }
                    
                    if (!File.Exists(templatePath))
                        throw new Exception("Шаблон договора не найден по пути: Documents/Договор_Юридических_Услуг.docx");
                }
                
                // Show save dialog to select output location
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Документ Word (*.docx)|*.docx",
                    Title = "Сохранить договор",
                    FileName = $"Договор_{person.LastName}_{DateTime.Now:dd.MM.yyyy}.docx"
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
                    { "<дата_выдачи>", passport.IssueDate.ToString("dd.MM.yyyy") },
                    { "<адрес_регистрации_заказчика>", registrationAddress.AddressText },
                    { "<фио_представителя_исполнителя>", "Иванов Иван Иванович" }, // Placeholder, should be retrieved from company data
                    { "<основание_действий_представителя>", "Доверенность №123 от 01.01.2023" }, // Placeholder, should be retrieved from company data
                    { "<cтоимость_договора>", contractTotalCost.ToString("#,##0.00") },
                    { "<стоимость_договора_прописью>", contractTotalCostWords },
                    { "<сумма_обязательных_расходов>", mandatoryExpenses.ToString("#,##0.00") },
                    { "<сумма_обязательных_расходов_прописью>", mandatoryExpensesWords },
                    { "<размер_вознаграждения_фин_управляющего>", managerFee.ToString("#,##0.00") },
                    { "<прочие_расходы_банкротства>", otherExpenses.ToString("#,##0.00") },
                    { "<номер_телефона_заказчика>", person.Phone ?? "-" },
                    { "<электронный_адрес_заказчика>", person.Email ?? "-" }
                };
                
                // Replace tags in document
                using (WordprocessingDocument document = WordprocessingDocument.Open(outputPath, true))
                {
                    foreach (var replacement in replacements)
                    {
                        ReplaceTextInDocument(document, replacement.Key, replacement.Value);
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
        
        private void ReplaceTextInDocument(WordprocessingDocument document, string searchText, string replaceText)
        {
            var body = document.MainDocumentPart.Document.Body;
            if (body == null) return;

            // Iterate through all paragraphs
            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                foreach (var run in paragraph.Descendants<Run>())
                {
                    foreach (var text in run.Descendants<Text>())
                    {
                        if (text.Text.Contains(searchText))
                        {
                            text.Text = text.Text.Replace(searchText, replaceText);
                        }
                    }
                }
            }

            // Check headers and footers
            foreach (var headerPart in document.MainDocumentPart.HeaderParts)
            {
                foreach (var paragraph in headerPart.Header.Descendants<Paragraph>())
                {
                    foreach (var run in paragraph.Descendants<Run>())
                    {
                        foreach (var text in run.Descendants<Text>())
                        {
                            if (text.Text.Contains(searchText))
                            {
                                text.Text = text.Text.Replace(searchText, replaceText);
                            }
                        }
                    }
                }
            }

            foreach (var footerPart in document.MainDocumentPart.FooterParts)
            {
                foreach (var paragraph in footerPart.Footer.Descendants<Paragraph>())
                {
                    foreach (var run in paragraph.Descendants<Run>())
                    {
                        foreach (var text in run.Descendants<Text>())
                        {
                            if (text.Text.Contains(searchText))
                            {
                                text.Text = text.Text.Replace(searchText, replaceText);
                            }
                        }
                    }
                }
            }
        }
    }
}