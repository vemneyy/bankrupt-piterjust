using System;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
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
                    { "<дата_выдачи>", passport.IssueDate.ToString("dd.MM.yyyy") },
                    { "<адрес_регистрации_заказчика>", registrationAddress.AddressText },
                    { "<фио_представителя_исполнителя>", representativeName },
                    { "<основание_действий_представителя>", representativeBasis },
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
                    bool anyTagsFound = false;
                    
                    foreach (var replacement in replacements)
                    {
                        bool tagFound = ReplaceTextInDocument(document, replacement.Key, replacement.Value);
                        if (tagFound)
                            anyTagsFound = true;
                    }
                    
                    if (!anyTagsFound)
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
        /// Replaces a specific text tag in a Word document with a replacement value.
        /// Handles cases where the tag might be split across multiple text runs.
        /// </summary>
        /// <returns>True if the tag was found and replaced, false otherwise</returns>
        private bool ReplaceTextInDocument(WordprocessingDocument document, string searchText, string replaceText)
        {
            bool tagFound = false;
            
            // Process the main document body
            if (document.MainDocumentPart?.Document?.Body != null)
            {
                tagFound |= ProcessTextElements(document.MainDocumentPart.Document.Body, searchText, replaceText);
            }
            
            // Process headers
            foreach (var headerPart in document.MainDocumentPart.HeaderParts)
            {
                tagFound |= ProcessTextElements(headerPart.Header, searchText, replaceText);
            }
            
            // Process footers
            foreach (var footerPart in document.MainDocumentPart.FooterParts)
            {
                tagFound |= ProcessTextElements(footerPart.Footer, searchText, replaceText);
            }
            
            // Process any other document parts that might contain text
            if (document.MainDocumentPart.FootnotesPart != null)
            {
                tagFound |= ProcessTextElements(document.MainDocumentPart.FootnotesPart.Footnotes, searchText, replaceText);
            }
            
            if (document.MainDocumentPart.EndnotesPart != null)
            {
                tagFound |= ProcessTextElements(document.MainDocumentPart.EndnotesPart.Endnotes, searchText, replaceText);
            }
            
            return tagFound;
        }
        
        /// <summary>
        /// Processes all text elements in a given OpenXML element and replaces the search text
        /// </summary>
        private bool ProcessTextElements(OpenXmlElement element, string searchText, string replaceText)
        {
            bool tagFound = false;
            
            // First attempt: Simple replacement within each Text element
            foreach (var text in element.Descendants<Text>())
