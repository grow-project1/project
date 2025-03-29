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
            Document doc = new Document(PageSize.A4);
            PdfWriter.GetInstance(doc, memoryStream);
            doc.Open();

            // Adicionar Título
            doc.Add(new Paragraph("Fatura #" + fatura.Numero));
            doc.Add(new Paragraph("Data: " + fatura.Data.ToString("dd/MM/yyyy")));
            doc.Add(new Paragraph("Comprador: " + fatura.NomeComprador));
            doc.Add(new Paragraph("NIF: " + (string.IsNullOrEmpty(fatura.NIF) ? "N/D" : fatura.NIF)));
            doc.Add(new Paragraph(" "));
            doc.Add(new Paragraph("Item Leiloado: " + fatura.ItemLeiloado));
            doc.Add(new Paragraph("Valor Final: " + fatura.ValorFinal.ToString("C")));
            doc.Add(new Paragraph(" "));

            doc.Close();

            return memoryStream.ToArray();
        }
    }
}
