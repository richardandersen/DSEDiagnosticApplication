﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Common;
using OfficeOpenXml;
using DataTableToExcel;

namespace DSEDiagtnosticToExcel
{
    public sealed class ApplicationInfoExcel : LoadToExcel
    {
        public const string DataTableName = "Application";

        public sealed class ApplInfo
        {
            public string ApplicationName;
            public string ApplicationVersion;
            public string ApplicationAssemblyDir;
            public string WorkingDir;
            public DateTimeRange ApplicationStartEndTime;
            public string ApplicationArgs;
            public string ApplicationLibrarySettings;
            public string DiagnosticDirectory;
            public bool Aborted;
            public int Errors;
            public int Warnings;
            public bool UnhandledException;
            public string DataQuality;

            public sealed class ResultInfo
            {
                public string MapperClass;
                public int MapperId;
                public string Catagory;
                public string DataCenter;
                public string Node;
                public int NbrTasksCompleted;
                public int NbrItemsParsed;
                public int NbrItemsGenerated;
                public int NbrTasksCanceled;
                public int NbrWarnings;
                public int NbrErrors;
                public int NbrExceptions;
            }

            public IList<ResultInfo> Results { get; } = new List<ResultInfo>();
        }

        public ApplicationInfoExcel(IFilePath excelTargetWorkbook,
                                        IFilePath excelTemplateWorkbook,
                                        string worksheetName)
            : base(new DataTable(DataTableName, DSEDiagnosticToDataTable.TableNames.Namespace),
                    excelTargetWorkbook,
                    excelTemplateWorkbook,
                    worksheetName,
                    true)
        { }

        public ApplicationInfoExcel(IFilePath excelTargetWorkbook, IFilePath excelTemplateWorkbook = null)
            : this(excelTargetWorkbook,
                    excelTemplateWorkbook,
                    null)
        { }

        public ApplInfo ApplicationInfo { get; } = new ApplInfo();

        public override Tuple<IFilePath, string, int> Load()
        {
            bool hasException = false;

            #region Update DataTable with ResultInfo

            this.CallActionEvent("PreLoading");

            this.DataTable.Columns.Add("Mapper Class", typeof(string));//b
            this.DataTable.Columns.Add("Mapper Id", typeof(int));//c
            this.DataTable.Columns.Add("Catagory", typeof(string));
            this.DataTable.Columns.Add("Data Center", typeof(string)).AllowDBNull = true; //e
            this.DataTable.Columns.Add("Node", typeof(string)).AllowDBNull = true; //f
            this.DataTable.Columns.Add("Nbr Tasks Completed", typeof(int)); //g
            this.DataTable.Columns.Add("Nbr Items Parsed", typeof(int));
            this.DataTable.Columns.Add("Nbr Items Generated", typeof(int)); //i
            this.DataTable.Columns.Add("Nbr Tasks Canceled", typeof(int));
            this.DataTable.Columns.Add("Nbr Warnings", typeof(int));
            this.DataTable.Columns.Add("Nbr Errors", typeof(int)); //l
            this.DataTable.Columns.Add("Nbr Exceptions", typeof(int));//m

            foreach (var item in this.ApplicationInfo.Results)
            {
                var dataRow = this.DataTable.NewRow();

                dataRow.SetField("Mapper Class", item.MapperClass);
                dataRow.SetField("Mapper Id", item.MapperId);
                dataRow.SetField("Catagory", item.Catagory);
                dataRow.SetField("Data Center", item.DataCenter);
                dataRow.SetField("Node", item.Node);
                dataRow.SetField("Nbr Tasks Completed", item.NbrTasksCompleted);
                dataRow.SetField("Nbr Items Parsed", item.NbrItemsParsed);
                dataRow.SetField("Nbr Items Generated", item.NbrItemsGenerated);
                dataRow.SetField("Nbr Tasks Canceled", item.NbrTasksCanceled);
                dataRow.SetField("Nbr Warnings", item.NbrWarnings);
                dataRow.SetField("Nbr Errors", item.NbrErrors);
                dataRow.SetField("Nbr Exceptions", item.NbrExceptions);

                if(!hasException)
                    hasException = item.NbrExceptions > 0;

                this.DataTable.Rows.Add(dataRow);
            }

            #endregion

            var nbrRows = DataTableToExcel.Helpers.WorkBook(this.ExcelTargetWorkbook.PathResolved, this.WorkSheetName, this.DataTable,
                                                           (stage, orgFilePath, targetFilePath, workSheetName, excelPackage, excelDataTable, rowCount, loadRange) =>
                                                           {
                                                               switch (stage)
                                                               {
                                                                   case WorkBookProcessingStage.PreProcess:
                                                                   case WorkBookProcessingStage.PrepareFileName:
                                                                   case WorkBookProcessingStage.PreProcessDataTable:
                                                                       break;
                                                                   case WorkBookProcessingStage.PreLoad:
                                                                       this.CallActionEvent("Begin Loading");
                                                                       break;
                                                                   case WorkBookProcessingStage.PreSave:
                                                                       {
                                                                           var workSheet = excelPackage.Workbook.Worksheets[WorkSheetName];
                                                                           var rangeAddress = loadRange == null ? null : workSheet?.Cells[loadRange];

                                                                           if(rangeAddress != null)
                                                                           {
                                                                               var startRow = rangeAddress.Start.Row;
                                                                               var endRow = rangeAddress.End.Row;
                                                                               bool canceled = false; //j
                                                                               bool errors = false; //k
                                                                               bool exceptions = false; //L
                                                                               bool notCompleted = false; //F
                                                                               bool warnings = false; //I

                                                                               for(int nRow = startRow; nRow <= endRow; ++nRow)
                                                                               {
                                                                                   if(workSheet.Cells[nRow,2] != null)
                                                                                   {
                                                                                       //J
                                                                                       if(workSheet.Cells[nRow, 10].Value is int &&  ((int)workSheet.Cells[nRow, 10].Value) > 0)
                                                                                       {
                                                                                           workSheet.Cells[nRow, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                                                                           workSheet.Cells[nRow, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                                                                                           workSheet.Cells[nRow, 10].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                                                                           workSheet.Cells[nRow, 10].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
                                                                                           warnings = true;
                                                                                       }
                                                                                       //I
                                                                                       if (workSheet.Cells[nRow, 9].Value is int && ((int)workSheet.Cells[nRow, 9].Value) > 0)
                                                                                       {
                                                                                           workSheet.Cells[nRow, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                                                                           workSheet.Cells[nRow, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                                                                                           workSheet.Cells[nRow, 9].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                                                                           workSheet.Cells[nRow, 9].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                                                                                           canceled = true;
                                                                                       }
                                                                                       //K
                                                                                       if (workSheet.Cells[nRow, 11].Value is int && ((int)workSheet.Cells[nRow, 11].Value) > 0)
                                                                                       {
                                                                                           workSheet.Cells[nRow, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                                                                           workSheet.Cells[nRow, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                                                                                           workSheet.Cells[nRow, 11].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                                                                           workSheet.Cells[nRow, 11].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                                                                                           errors = true;
                                                                                       }
                                                                                       //L
                                                                                       if (workSheet.Cells[nRow, 12].Value is int && ((int)workSheet.Cells[nRow, 12].Value) > 0)
                                                                                       {
                                                                                           workSheet.Cells[nRow, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                                                                           workSheet.Cells[nRow, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                                                                                           workSheet.Cells[nRow, 12].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                                                                           workSheet.Cells[nRow, 12].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                                                                                           exceptions = true;
                                                                                       }
                                                                                       //F
                                                                                       if (workSheet.Cells[nRow, 6].Value is int && ((int)workSheet.Cells[nRow, 6].Value) == 0)
                                                                                       {
                                                                                           workSheet.Cells[nRow, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                                                                           workSheet.Cells[nRow, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                                                                                           workSheet.Cells[nRow, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                                                                                           workSheet.Cells[nRow, 6].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Red);
                                                                                           notCompleted = true;
                                                                                       }
                                                                                   }
                                                                                   workSheet.Row(nRow).OutlineLevel = 2;
                                                                                   //workSheet.Row(nRow).Collapsed = false;
                                                                               }

                                                                               if(errors)
                                                                               {
                                                                                   workSheet.Cells[14, 11].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.LightGray;
                                                                                   workSheet.Cells[14, 11].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.OrangeRed);
                                                                               }
                                                                               if(exceptions)
                                                                               {
                                                                                   workSheet.Cells[14, 12].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.LightGray;
                                                                                   workSheet.Cells[14, 12].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.OrangeRed);
                                                                               }
                                                                               if(notCompleted)
                                                                               {
                                                                                   workSheet.Cells[14, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.LightGray;
                                                                                   workSheet.Cells[14, 6].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.OrangeRed);
                                                                               }
                                                                               if(canceled)
                                                                               {
                                                                                   workSheet.Cells[14, 9].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.LightGray;
                                                                                   workSheet.Cells[14, 9].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.OrangeRed);
                                                                               }
                                                                               if (warnings)
                                                                               {
                                                                                   workSheet.Cells[14, 10].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.LightGray;
                                                                                   workSheet.Cells[14, 10].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                                                                               }

                                                                               if (errors)
                                                                               {
                                                                                   workSheet.Cells[14, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.LightGray;
                                                                                   workSheet.Cells[14, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.OrangeRed);
                                                                               }
                                                                               else if (exceptions)
                                                                               {
                                                                                   workSheet.Cells[14, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.LightGray;
                                                                                   workSheet.Cells[14, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.OrangeRed);
                                                                               }
                                                                               else if (notCompleted)
                                                                               {
                                                                                   workSheet.Cells[14, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.LightGray;
                                                                                   workSheet.Cells[14, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.OrangeRed);
                                                                               }
                                                                               else if (canceled)
                                                                               {
                                                                                   workSheet.Cells[14, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.LightGray;
                                                                                   workSheet.Cells[14, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.OrangeRed);
                                                                               }
                                                                               else if (warnings)
                                                                               {
                                                                                   workSheet.Cells[14, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.LightGray;
                                                                                   workSheet.Cells[14, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                                                                               }
                                                                           }

                                                                           this.CallActionEvent("Loaded");
                                                                       }
                                                                       break;
                                                                   case WorkBookProcessingStage.Saved:
                                                                       this.CallActionEvent("Workbook Saved");
                                                                       break;
                                                                   case WorkBookProcessingStage.PostProcess:
                                                                       break;
                                                                   default:
                                                                       break;
                                                               }
                                                           },
                                                            (workSheet, splitNbr) =>
                                                            {
                                                                //workSheet.Column(1).Width = 100;
                                                                //workSheet.Column(1).Style.WrapText = true;
                                                                //workSheet.Column(1).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;

                                                                if (!this.AppendToWorkSheet)
                                                                {
                                                                    if(this.ApplicationInfo.UnhandledException)
                                                                        workSheet.Cells["A2"].Value =  "** UnHandled Exception(s) Detected **";
                                                                    else
                                                                        workSheet.Cells["A2"].Value = this.ApplicationInfo.Aborted ? "** Aborted **" : (hasException ? "** Exception(s) Detected **" : null);

                                                                    workSheet.Cells["A3"].Value = this.ApplicationInfo.ApplicationName;
                                                                    workSheet.Cells["A4"].Value = this.ApplicationInfo.ApplicationVersion;
                                                                    workSheet.Cells["A5"].Value = this.ApplicationInfo.ApplicationAssemblyDir;
                                                                    workSheet.Cells["A6"].Value = "Application Arguments:\r\n\t"
                                                                                                    + this.ApplicationInfo.ApplicationArgs.Replace(" -", "\r\n\t-");
                                                                    workSheet.Cells["A7"].Value = "Library Settings:\r\n"
                                                                                                    + this.ApplicationInfo.ApplicationLibrarySettings;
                                                                    if (this.ApplicationInfo.ApplicationStartEndTime != null)
                                                                    {
                                                                        workSheet.Cells["A8"].Value = string.Format("Started: {0:yyyy-MM-dd\\ HH\\:mm\\:ss}\r\nEnded: {1:yyyy-MM-dd\\ HH\\:mm\\:ss}\r\nDuration: {2:d\\ hh\\:mm}",
                                                                                                                    this.ApplicationInfo.ApplicationStartEndTime.Min,
                                                                                                                    this.ApplicationInfo.ApplicationStartEndTime.Max,
                                                                                                                    this.ApplicationInfo.ApplicationStartEndTime.TimeSpan());
                                                                    }
                                                                    workSheet.Cells["A9"].Value = "Working Directory: " + this.ApplicationInfo.WorkingDir;
                                                                    workSheet.Cells["A10"].Value = "Diagnostic Directory: " + this.ApplicationInfo.DiagnosticDirectory;
                                                                    workSheet.Cells["A11"].Value = string.Format("Errors: {0:###,###,##0} Warnings: {1:###,###,##0}", this.ApplicationInfo.Errors, this.ApplicationInfo.Warnings);
                                                                    workSheet.Cells["A12"].Value = string.Format("Data Quality: {0}", this.ApplicationInfo.DataQuality);

                                                                    for (int nRow = 1; nRow <= 12; ++nRow)
                                                                    {
                                                                        workSheet.Row(nRow).OutlineLevel = 1;
                                                                        //workSheet.Row(nRow).Collapsed = false;
                                                                    }
                                                                    //workSheet.Column(1).OutlineLevel = 1;
                                                                    //workSheet.Column(1).Collapsed = false;
                                                                }

                                                                //DataTable
                                                                workSheet.Cells["14:14"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.LightGray;
                                                                workSheet.Cells["14:14"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                                                                //workSheet.View.FreezePanes(3, 1);

                                                                workSheet.Cells["F:F"].Style.Numberformat.Format = "#,###,###,##0";
                                                                workSheet.Cells["G:G"].Style.Numberformat.Format = "#,###,###,##0";
                                                                workSheet.Cells["H:H"].Style.Numberformat.Format = "#,###,###,##0";
                                                                workSheet.Cells["I:I"].Style.Numberformat.Format = "#,###,###,##0";
                                                                workSheet.Cells["J:J"].Style.Numberformat.Format = "#,###,###,##0";
                                                                workSheet.Cells["K:K"].Style.Numberformat.Format = "#,###,###,##0";
                                                                workSheet.Cells["L:L"].Style.Numberformat.Format = "#,###,###,##0";

                                                                workSheet.Cells["A14:L14"].AutoFilter = true;

                                                                workSheet.AutoFitColumn(workSheet.Cells["B:L"]);
                                                            },
                                                            -1,
                                                           -1,
                                                           "A14",
                                                           this.UseDataTableDefaultView,
                                                           appendToWorkSheet: this.AppendToWorkSheet,
                                                           clearWorkSheet: false,
                                                           cachePackage: LibrarySettings.ExcelPackageCache,
                                                           //saveWorkSheet: LibrarySettings.ExcelSaveWorkSheet,
                                                           excelTemplateFile: this.ExcelTemplateWorkbook);

            return new Tuple<IFilePath, string, int>(this.ExcelTargetWorkbook, this.WorkSheetName, nbrRows);
        }
    }
}
