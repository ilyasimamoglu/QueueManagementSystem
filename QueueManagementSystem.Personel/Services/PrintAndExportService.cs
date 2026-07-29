using ClosedXML.Excel;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using QueueManagementSystem.Counter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace QueueManagementSystem.Counter.Services
{
    public class PrintAndExportService
    {
        public string ExportToExcel(List<StaffPerformanceDto> perfData)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fileName = $"PersonelRaporu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string filePath = Path.Combine(desktopPath, fileName);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Performans Raporu");

                worksheet.Cell(1, 1).Value = "Personel Adı";
                worksheet.Cell(1, 2).Value = "Baktığı Kişi";
                worksheet.Cell(1, 3).Value = "Ort. Süre";
                worksheet.Cell(1, 4).Value = "Mola Süresi";
                worksheet.Cell(1, 5).Value = "Durum";

                var headerRange = worksheet.Range("A1:E1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                for (int i = 0; i < perfData.Count; i++)
                {
                    int row = i + 2;
                    worksheet.Cell(row, 1).Value = perfData[i].Name;
                    worksheet.Cell(row, 2).Value = perfData[i].Served;
                    worksheet.Cell(row, 3).Value = perfData[i].AvgTime;
                    worksheet.Cell(row, 4).Value = perfData[i].BreakTime;
                    worksheet.Cell(row, 5).Value = perfData[i].Status;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            }
            return fileName;
        }

        public string ExportToPdf(List<StaffPerformanceDto> perfData)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fileName = $"PersonelRaporu_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            string filePath = Path.Combine(desktopPath, fileName);

            using (PdfWriter writer = new PdfWriter(filePath))
            using (PdfDocument pdf = new PdfDocument(writer))
            {
                iText.Layout.Document document = new iText.Layout.Document(pdf);
                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                // iText sınıflarını tam adresleriyle belirttik (Çakışma önlendi)
                iText.Layout.Element.Paragraph header = new iText.Layout.Element.Paragraph("Personel Performans Raporu")
                    .SetFont(boldFont)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetFontSize(20);
                document.Add(header);

                document.Add(new iText.Layout.Element.Paragraph($"Olusturulma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}\n\n")
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT));

                iText.Layout.Element.Table table = new iText.Layout.Element.Table(5, true);

                table.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("Personel Adi").SetFont(boldFont)));
                table.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("Baktigi Kisi").SetFont(boldFont)));
                table.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("Ort. Sure").SetFont(boldFont)));
                table.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("Mola Suresi").SetFont(boldFont)));
                table.AddHeaderCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph("Durum").SetFont(boldFont)));

                foreach (var item in perfData)
                {
                    table.AddCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph(item.Name)));
                    table.AddCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph(item.Served)));
                    table.AddCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph(item.AvgTime)));
                    table.AddCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph(item.BreakTime)));
                    table.AddCell(new iText.Layout.Element.Cell().Add(new iText.Layout.Element.Paragraph(item.Status)));
                }

                document.Add(table);
                document.Close();
            }
            return fileName;
        }

        public void PrintTestTicket(string printerName)
        {
            PrintDialog printDialog = new PrintDialog();
            printDialog.PrintQueue = new LocalPrintServer().GetPrintQueue(printerName);

            System.Windows.Documents.FlowDocument doc = new System.Windows.Documents.FlowDocument();
            doc.PageWidth = 300;
            doc.PagePadding = new Thickness(10, 0, 10, 0);
            doc.FontFamily = new System.Windows.Media.FontFamily("Consolas");

            System.Windows.Documents.Paragraph header = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("GİŞE PRO - DONANIM TESTİ"))
            {
                TextAlignment = System.Windows.TextAlignment.Center,
                FontWeight = System.Windows.FontWeights.Bold,
                FontSize = 16,
                Margin = new Thickness(0, 10, 0, 5)
            };
            doc.Blocks.Add(header);

            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("================================")) { TextAlignment = System.Windows.TextAlignment.Center, Margin = new Thickness(0) });

            System.Windows.Documents.Paragraph ticketNo = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("TEST-01"))
            {
                TextAlignment = System.Windows.TextAlignment.Center,
                FontWeight = System.Windows.FontWeights.Bold,
                FontSize = 48,
                Margin = new Thickness(0, 5, 0, 5)
            };
            doc.Blocks.Add(ticketNo);

            doc.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("================================")) { TextAlignment = System.Windows.TextAlignment.Center, Margin = new Thickness(0) });

            System.Windows.Documents.Paragraph info = new System.Windows.Documents.Paragraph()
            {
                TextAlignment = System.Windows.TextAlignment.Center,
                FontSize = 11,
                Margin = new Thickness(0, 8, 0, 5)
            };
            info.Inlines.Add(new System.Windows.Documents.Run($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\n"));
            info.Inlines.Add(new System.Windows.Documents.Run("Yazıcı Bağlantısı Başarılı!"));
            doc.Blocks.Add(info);

            IDocumentPaginatorSource idpSource = doc;
            printDialog.PrintDocument(idpSource.DocumentPaginator, "Gişe Pro - Test Bileti");
        }
    }
}