using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using projeto.Models;

public class PdfService
{
    public byte[] GerarFaturaPDF(Fatura fatura)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            Document doc = new Document(PageSize.A4, 50, 50, 50, 50);
            PdfWriter.GetInstance(doc, memoryStream);
            doc.Open();

            // Adicionar Imagem da Empresa
            string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logoProjeto.png");

            if (File.Exists(imagePath))
            {
                Image logo = Image.GetInstance(imagePath);
                logo.ScaleToFit(140, 70);
                logo.Alignment = Element.ALIGN_CENTER;
                doc.Add(logo);
            }

            doc.Add(new Paragraph(" "));
            doc.Add(new Paragraph("FATURA #" + fatura.Numero, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.BLACK))
            {
                Alignment = Element.ALIGN_CENTER
            });
            doc.Add(new Paragraph("Data: " + fatura.Data.ToString("dd/MM/yyyy"), FontFactory.GetFont(FontFactory.HELVETICA, 12)));
            doc.Add(new Paragraph("Comprador: " + fatura.NomeComprador, FontFactory.GetFont(FontFactory.HELVETICA, 12)));
            doc.Add(new Paragraph("NIF: " + (string.IsNullOrEmpty(fatura.NIF) ? "N/D" : fatura.NIF), FontFactory.GetFont(FontFactory.HELVETICA, 12)));
            doc.Add(new Paragraph(" "));

            // Criar tabela para detalhes da compra
            PdfPTable table = new PdfPTable(2);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 70, 30 });

            // Cabeçalhos da tabela
            table.AddCell(new PdfPCell(new Phrase("Descrição", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12))) { HorizontalAlignment = Element.ALIGN_LEFT, BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase("Valor (€)", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12))) { HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = BaseColor.LIGHT_GRAY });

            // Detalhes do item
            table.AddCell(new PdfPCell(new Phrase(fatura.ItemLeiloado, FontFactory.GetFont(FontFactory.HELVETICA, 12))));
            table.AddCell(new PdfPCell(new Phrase(fatura.ValorBase.ToString("C"), FontFactory.GetFont(FontFactory.HELVETICA, 12))) { HorizontalAlignment = Element.ALIGN_RIGHT });

            // IVA
            table.AddCell(new PdfPCell(new Phrase("IVA (23%)", FontFactory.GetFont(FontFactory.HELVETICA, 12))));
            table.AddCell(new PdfPCell(new Phrase(fatura.IVA.ToString("C"), FontFactory.GetFont(FontFactory.HELVETICA, 12))) { HorizontalAlignment = Element.ALIGN_RIGHT });

            // Se houver desconto
            if (fatura.Desconto > 0)
            {
                table.AddCell(new PdfPCell(new Phrase("Desconto aplicado", FontFactory.GetFont(FontFactory.HELVETICA, 12))));
                table.AddCell(new PdfPCell(new Phrase("-" + fatura.Desconto.ToString("C"), FontFactory.GetFont(FontFactory.HELVETICA, 12))) { HorizontalAlignment = Element.ALIGN_RIGHT });
            }

            // Total a pagar
            table.AddCell(new PdfPCell(new Phrase("Total a Pagar", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12))) { BackgroundColor = BaseColor.LIGHT_GRAY });
            table.AddCell(new PdfPCell(new Phrase(fatura.TotalComIVA.ToString("C"), FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12))) { HorizontalAlignment = Element.ALIGN_RIGHT, BackgroundColor = BaseColor.LIGHT_GRAY });

            doc.Add(table);
            doc.Add(new Paragraph(" "));

            // Rodapé
            doc.Add(new Paragraph("Obrigado pela sua compra!", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)) { Alignment = Element.ALIGN_CENTER });

            doc.Close();

            return memoryStream.ToArray();
        }
    }
}
