using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace OmicIntegrator.Helpers
{
    public class ExcelReader : IDisposable
    {
        XSSFWorkbook book;
        ISheet sheet;
        int currRowIndex = -1;
        public List<string> Headers { get; }
        public ExcelReader(string FilePath, bool WithHeaders, string SheetName = null)
        {
            book = new(FilePath, true);

            List<string> sheetNames = [];
            for (int h = 0; h < book.NumberOfSheets; h++)
            {
                sheetNames.Add(book.GetSheetAt(h).SheetName);
            }

            sheet = !string.IsNullOrEmpty(SheetName) ? book.GetSheet(SheetName) : book.GetSheetAt(0);

            if (WithHeaders)
            {
                currRowIndex = 0;
                Headers = sheet.GetRow(currRowIndex).Select(c => c.StringCellValue).ToList();
            }
        }
        public bool ReadNextRow(out Row Row)
        {
            if (sheet.LastRowNum > currRowIndex)
            {
                currRowIndex++;

                var fileRow = sheet.GetRow(currRowIndex);

                Row = new(fileRow, this);

                return true;
            }

            Row = null;

            return false;
        }
        public void Dispose()
        {
            book.Dispose();
        }
        public class Row
        {
            private readonly IRow fileRow;

            public ExcelReader Reader { get; }

            public Row(IRow fileRow, ExcelReader Reader)
            {
                this.fileRow = fileRow;
                this.Reader = Reader;
            }

            public TValue GetCellByHeader<TValue>(string Header)
            {
                var col = Reader.Headers.IndexOf(Header);

                var cell = fileRow.GetCell(col);

                if (cell == null)
                    return default;

                var nullableType = Nullable.GetUnderlyingType(typeof(TValue));

                if (nullableType != null)
                {
                    var thisMethod = typeof(Row).GetMethod(nameof(GetCellByHeader)).MakeGenericMethod(nullableType);

                    var cellVal = thisMethod.Invoke(this, [Header]);

                    return (TValue)cellVal;
                }
                else
                {
                    switch (typeof(TValue))
                    {
                        case var s when s == typeof(string):
                            return (TValue)(object)cell.StringCellValue;
                        case var i when i == typeof(int):
                            return (TValue)(object)(int)cell.NumericCellValue;
                        case var l when l == typeof(long):
                            return (TValue)(object)(long)cell.NumericCellValue;
                        case var d when d == typeof(decimal):
                            return (TValue)(object)(decimal)cell.NumericCellValue;
                        case var o when o == typeof(double):
                            return (TValue)(object)cell.NumericCellValue;
                        case var d when d == typeof(DateTime):
                            return (TValue)(object)cell.DateCellValue;
                        case var d when d == typeof(DateOnly):
                            return (TValue)(object)cell.DateOnlyCellValue;
                        case var b when b == typeof(bool):
                            return (TValue)(object)cell.BooleanCellValue;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }
    }
}
