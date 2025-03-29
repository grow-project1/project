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

            // Adicionar Título da Fatura
            doc.Add(new Paragraph("Fatura #" + fatura.Numero, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16)));
            doc.Add(new Paragraph("Data: " + fatura.Data.ToString("dd/MM/yyyy")));
            doc.Add(new Paragraph("Comprador: " + fatura.NomeComprador));
            doc.Add(new Paragraph("NIF: " + (string.IsNullOrEmpty(fatura.NIF) ? "N/D" : fatura.NIF)));
            doc.Add(new Paragraph(" ")); // Espaçamento

            // Detalhes do Leilão
            doc.Add(new Paragraph("Item Leiloado: " + fatura.ItemLeiloado));
            doc.Add(new Paragraph(" ")); // Espaçamento

            // Detalhes de valores
            doc.Add(new Paragraph("Valor Base (Sem IVA): " + fatura.ValorBase.ToString("C")));
            doc.Add(new Paragraph("IVA (23%): " + fatura.IVA.ToString("C")));

            // Se houver desconto, adicionar na fatura
            if (fatura.Desconto > 0)
            {
                doc.Add(new Paragraph("Desconto: " + fatura.Desconto.ToString("C")));
            }

            // Valor total com IVA
            doc.Add(new Paragraph("Valor Total (Com IVA): " + fatura.TotalComIVA.ToString("C")));
            doc.Add(new Paragraph(" ")); // Espaçamento

            // Fechar o documento
            doc.Close();

            return memoryStream.ToArray();
        }
    }
}
