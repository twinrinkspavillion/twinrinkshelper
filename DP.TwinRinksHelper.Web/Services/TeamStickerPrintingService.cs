using DP.TwinRinksHelper.Web.Services;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Extensions.DependencyInjection;
using PdfRpt.Core.Contracts;
using PdfRpt.Core.Helper;
using PdfRpt.FluentInterface;
using System;
using System.Collections.Generic;
using System.IO;

namespace DP.TwinRinksHelper.Web.Services
{
    public class TeamStickerPrintingService
    {
        public byte[] CreateTeamLabelsPDFReport(TeamStickerDescriptor descr)
        {

            if (long.TryParse(descr.CoachPhone.Trim(), out long phoneLong))
            {
                descr.CoachPhone = string.Format("{0:(###) ###-####}", phoneLong);
            }

            PdfReport pdf = new PdfReport().DocumentPreferences(doc =>
            {
                doc.RunDirection(PdfRunDirection.LeftToRight);
                doc.Orientation(PageOrientation.Landscape);
                doc.PageSize(PdfPageSize.Letter);
                doc.DocumentMetadata(new DocumentMetadata { Author = "TwinRinks", Application = "PdfRpt", Keywords = "Labels", Subject = "Labels Avery 8163", Title = $"{descr.TeamName} Stickers" });
                doc.DocumentMargins(new DocumentMargins() { Left = 24, Top = 1 });

                doc.Compression(new CompressionSettings
                {
                    EnableCompression = true,
                    EnableFullCompression = true
                });
            })
            .DefaultFonts(fonts =>
            {
                fonts.Size(8);
                fonts.Color(System.Drawing.Color.Black);
            })
            .MainTablePreferences(table =>
            {
                table.ColumnsWidthsType(TableColumnWidthType.EquallySized);
                table.ShowHeaderRow(false);
                table.MultipleColumnsPerPage(new MultipleColumnsPerPage
                {
                    ColumnsGap = 0,
                    ColumnsPerPage = 5,
                    ColumnsWidth = 145,
                    IsRightToLeft = false,
                    TopMargin = 0,


                });
            })
            .MainTableDataSource(dataSource =>
            {
                dataSource.StronglyTypedList<TeamTemplateDTO>(TeamTemplateDTO.BuildTeams(descr));
            })
            .MainTableColumns(columns =>
            {
                columns.AddColumn(column =>
                {
                    column.PropertyName<TeamTemplateDTO>(x => x.CoachName);
                    column.CellsHorizontalAlignment(HorizontalAlignment.Center);
                    column.IsVisible(true);
                    column.FixedHeight(280);
                    column.ColumnItemsTemplate(t => t.CustomTemplate(
                        new MailingLabelCellTemplate()));
                });
            })
            .MainTableEvents(events =>
            {
                events.DataSourceIsEmpty(message: "There is no data available to display.");
            });

            return pdf.GenerateAsByteArray();
        }


        public class MailingLabelCellTemplate : IColumnItemsTemplate
        {
            private static readonly byte[] SharksLogo = ExtractResource("DP.TwinRinksHelper.Web.Content.sharkslogo_print.gif");
            /// <summary>
            ///
            /// </summary>
            public CellBasicProperties BasicProperties { set; get; }

            /// <summary>
            /// Defines the current cell's properties, based on the other cells values.
            /// Here IList contains actual row's cells values.
            /// It can be null.
            /// </summary>
            public Func<IList<CellData>, CellBasicProperties> ConditionalFormatFormula { set; get; }

            /// <summary>
            ///
            /// </summary>
            /// <returns></returns>
            public PdfPCell RenderingCell(CellAttributes attributes)
            {

                PdfPCell pdfCell = new PdfPCell() ;

                PdfGrid table = new PdfGrid(1) { RunDirection = PdfWriter.RUN_DIRECTION_LTR };

                iTextSharp.text.Image photo = PdfImageHelper.GetITextSharpImageFromByteArray(SharksLogo);

                photo.WidthPercentage = 60;

                table.AddCell(new PdfPCell(photo, true) { Border = 0 });


                string coachName = attributes.RowData.TableRowData[0].PropertyValue.ToSafeString();
                string coachPhone = attributes.RowData.TableRowData[1].PropertyValue.ToSafeString();

                List<string> players = attributes.RowData.TableRowData[2].PropertyValue as List<string>;

                foreach (string p in players)
                {
                    table.AddCell(new PdfPCell(attributes.BasicProperties.PdfFont.FontSelector.Process(p)) { Border = 0 });
                }

                table.AddCell(new PdfPCell(attributes.BasicProperties.PdfFont.FontSelector.Process("")) { Border = 0 });
                table.AddCell(new PdfPCell(attributes.BasicProperties.PdfFont.FontSelector.Process("")) { Border = 0 });

                if (!string.IsNullOrWhiteSpace(coachName))
                {
                    table.AddCell(new PdfPCell(attributes.BasicProperties.PdfFont.FontSelector.Process("Head Coach")) { Border = 0 });

                    table.AddCell(new PdfPCell(attributes.BasicProperties.PdfFont.FontSelector.Process(coachName + " " + coachPhone)) { Border = 1 });

                }

                pdfCell.AddElement(table);

                return pdfCell;
            }

            public void CellRendered(PdfPCell cell, iTextSharp.text.Rectangle position, PdfContentByte[] canvases, CellAttributes attributes)
            {

            }

            public static byte[] ExtractResource(string filename)
            {
                System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                using (Stream resFilestream = a.GetManifestResourceStream(filename))
                {
                    if (resFilestream == null)
                    {
                        return null;
                    }

                    byte[] ba = new byte[resFilestream.Length];
                    resFilestream.Read(ba, 0, ba.Length);
                    return ba;
                }
            }
        }
        public class TeamStickerDescriptor
        {
            public class PlayerDescriptor
            {
                public string PlayerNumber { get; set; }
                public string PlayerName { get; set; }
            }
            public List<PlayerDescriptor> Players { get; set; }
            public string CoachName { get; set; }
            public string CoachPhone { get; set; }
            public string TeamName { get; set; }
        }
        private class TeamTemplateDTO
        {
            public List<string> Players;
            public string CoachName { get; set; }
            public string CoachPhone { get; set; }
            public static List<TeamTemplateDTO> BuildTeams(TeamStickerDescriptor descr)
            {
                List<string> players = new List<string>();

                foreach (TeamStickerDescriptor.PlayerDescriptor p in descr.Players)
                {
                    if (string.IsNullOrWhiteSpace(p.PlayerNumber))
                    {
                        players.Add($"{p.PlayerName}");
                    }
                    else
                    {
                        players.Add($"{p.PlayerNumber.ToString().PadLeft(2)} - {p.PlayerName}");
                    }
                }

                List<TeamTemplateDTO> r = new List<TeamTemplateDTO>();

                for (int i = 0; i < 10; i++)
                {
                    r.Add(new TeamTemplateDTO() { Players = players, CoachName = descr.CoachName, CoachPhone = descr.CoachPhone });
                }

                return r;
            }
        }
    }



}

public static class TeamStickerPrintingServiceExtentions
{
    public static IServiceCollection AddSendGrid(this IServiceCollection me)
    {
        me.AddSingleton<TeamStickerPrintingService>();

        return me;

    }
}