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

            // Logo
            string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logoProjeto.png");
            if (File.Exists(imagePath))
            {
                Image logo = Image.GetInstance(imagePath);
                logo.ScaleToFit(140, 70);
                logo.Alignment = Element.ALIGN_CENTER;
                doc.Add(logo);
            }

            doc.Add(new Paragraph(" "));
            doc.Add(new Paragraph("FATURA #" + fatura.Numero, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16)) { Alignment = Element.ALIGN_CENTER });
            doc.Add(new Paragraph("---- Endereço de faturaçao -------------------------------- ", FontFactory.GetFont(FontFactory.HELVETICA, 10)));
            doc.Add(new Paragraph("Data: " + fatura.Data.ToString("dd/MM/yyyy"), FontFactory.GetFont(FontFactory.HELVETICA, 12)));
            doc.Add(new Paragraph("" + fatura.NomeComprador, FontFactory.GetFont(FontFactory.HELVETICA, 12)));
            doc.Add(new Paragraph("NIF: " + (string.IsNullOrEmpty(fatura.NIF) ? "N/D" : fatura.NIF), FontFactory.GetFont(FontFactory.HELVETICA, 12)));
            doc.Add(new Paragraph("" + fatura.Rua, FontFactory.GetFont(FontFactory.HELVETICA, 12)));
            doc.Add(new Paragraph("" + fatura.CodigoPostal, FontFactory.GetFont(FontFactory.HELVETICA, 12)));
            doc.Add(new Paragraph("" + fatura.Pais, FontFactory.GetFont(FontFactory.HELVETICA, 12)));
            doc.Add(new Paragraph("--------------------------------------------------------------------", FontFactory.GetFont(FontFactory.HELVETICA, 10)));
            doc.Add(new Paragraph(" "));

            // Verifica se tem desconto
            bool temDesconto = fatura.Desconto > 0;

            // Criar tabela dinâmica
            PdfPTable tabela;
            if (temDesconto)
            {
                tabela = new PdfPTable(7); // Com coluna de desconto
                tabela.SetWidths(new float[] { 30, 10, 15, 10, 10, 15, 15 });
            }
            else
            {
                tabela = new PdfPTable(6); // Sem coluna de desconto
                tabela.SetWidths(new float[] { 30, 10, 15, 10, 15, 15 });
            }

            tabela.WidthPercentage = 100;

            // Cabeçalhos da tabela
            string[] cabecalhosComDesconto = { "Descrição", "Quant.", "Preço s/IVA", "IVA", "Desc.", "Total s/IVA", "Total c/IVA" };
            string[] cabecalhosSemDesconto = { "Descrição", "Quant.", "Preço s/IVA", "IVA", "Total s/IVA", "Total c/IVA" };

            foreach (var cab in temDesconto ? cabecalhosComDesconto : cabecalhosSemDesconto)
            {
                tabela.AddCell(new PdfPCell(new Phrase(cab, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                {
                    BackgroundColor = BaseColor.LIGHT_GRAY,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5
                });
            }

            // Linha com os dados da fatura
            tabela.AddCell(new PdfPCell(new Phrase(fatura.ItemLeiloado)));
            tabela.AddCell(new PdfPCell(new Phrase("1")) { HorizontalAlignment = Element.ALIGN_CENTER });
            tabela.AddCell(new PdfPCell(new Phrase(fatura.ValorBase.ToString("F2") + " €")) { HorizontalAlignment = Element.ALIGN_RIGHT });
            tabela.AddCell(new PdfPCell(new Phrase("23%")) { HorizontalAlignment = Element.ALIGN_CENTER });

            if (temDesconto)
            {
                tabela.AddCell(new PdfPCell(new Phrase(fatura.Desconto.ToString("F2") + " €")) { HorizontalAlignment = Element.ALIGN_RIGHT });
            }

            decimal totalSemIVA = fatura.ValorBase - fatura.Desconto;

            tabela.AddCell(new PdfPCell(new Phrase(totalSemIVA.ToString("F2") + " €")) { HorizontalAlignment = Element.ALIGN_RIGHT });
            tabela.AddCell(new PdfPCell(new Phrase(fatura.TotalComIVA.ToString("F2") + " €")) { HorizontalAlignment = Element.ALIGN_RIGHT });

            doc.Add(tabela);
            doc.Add(new Paragraph(" "));

            // Resumo com campos personalizados
            PdfPTable resumo = new PdfPTable(2);
            resumo.WidthPercentage = 60;
            resumo.HorizontalAlignment = Element.ALIGN_RIGHT;
            resumo.SetWidths(new float[] { 60, 40 });

            // Valor Mercadoria
            resumo.AddCell(new PdfPCell(new Phrase("Valor Mercadoria:", FontFactory.GetFont(FontFactory.HELVETICA, 10))) { Border = Rectangle.NO_BORDER });
            resumo.AddCell(new PdfPCell(new Phrase(fatura.ValorBase.ToString("F2") + " €")) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT });

            // Descontos
            resumo.AddCell(new PdfPCell(new Phrase("Descontos:", FontFactory.GetFont(FontFactory.HELVETICA, 10))) { Border = Rectangle.NO_BORDER });
            resumo.AddCell(new PdfPCell(new Phrase(fatura.Desconto.ToString("F2") + " €")) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT });

            // Valor s/IVA
            resumo.AddCell(new PdfPCell(new Phrase("Valor s/IVA:", FontFactory.GetFont(FontFactory.HELVETICA, 10))) { Border = Rectangle.NO_BORDER });
            resumo.AddCell(new PdfPCell(new Phrase(totalSemIVA.ToString("F2") + " €")) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT });

            // Valor IVA
            resumo.AddCell(new PdfPCell(new Phrase("Valor IVA (23%):", FontFactory.GetFont(FontFactory.HELVETICA, 10))) { Border = Rectangle.NO_BORDER });
            resumo.AddCell(new PdfPCell(new Phrase(fatura.IVA.ToString("F2") + " €")) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT });

            // Total (EUR)
            resumo.AddCell(new PdfPCell(new Phrase("Total (EUR):", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11))) { Border = Rectangle.TOP_BORDER });
            resumo.AddCell(new PdfPCell(new Phrase(fatura.TotalComIVA.ToString("F2") + " €", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11))) { Border = Rectangle.TOP_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT });

            doc.Add(resumo);
            doc.Add(new Paragraph(" "));

            // Rodapé
            doc.Add(new Paragraph("Obrigado pela sua compra!", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)) { Alignment = Element.ALIGN_CENTER });

            doc.Close();
            return memoryStream.ToArray();
        }
    }

}


