using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data;
using System.Reflection;

namespace OmicIntegrator.Helpers
{
    public class ExcelWriter
    {

        public void Write<TRow>(string FileName, IEnumerable<TRow> Filas, bool ArrangeAddedCols = false)
        {
            List<Property<TRow>> Props;

            Func<TRow, object> PropGetter(MethodInfo prop) =>
                    fil =>
                    {
                        return prop.Invoke(fil, null);
                    };

            Func<PropertyInfo, bool> IsAddedCols =
                p => p.Name == "AddCols" && p.PropertyType == typeof(Dictionary<string, object>);

            var TRowProps = typeof(TRow)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Props = (from prop in TRowProps
                        where !IsAddedCols(prop)
                        select new Property<TRow>
                        {
                            Name = prop.Name,
                            ValGet = PropGetter(prop.GetGetMethod()),
                            IsVal = false
                        })
                    .ToList();

            var AddedColsProp = TRowProps.FirstOrDefault(IsAddedCols);
            if (AddedColsProp != null)
            {
                var Getter = PropGetter(AddedColsProp.GetGetMethod());

                var Keys = Filas.SelectMany(f => ((Dictionary<string, object>)Getter(f)).Keys).Distinct().ToList();

                if (ArrangeAddedCols)
                    Keys = Keys.Order().ToList();

                foreach (var k in Keys)
                {
                    Props.Add(new Property<TRow>
                    {
                        Name = k,
                        ValGet = f =>
                        {
                            var Dict = (Dictionary<string, object>)Getter(f);

                            Dict.TryGetValue(k, out var val);

                            return val;
                        },
                        IsVal = false
                    });
                }
            }

            IWorkbook book = new XSSFWorkbook();

            var SheetName = Path.GetFileNameWithoutExtension(FileName);
            if (SheetName.Length > 30)
                SheetName = SheetName.Substring(0, 30);

            var Sheet = book.CreateSheet(SheetName);

            var RowNum = 0;

            var Row = Sheet.CreateRow(RowNum);
            RowNum++;

            for (var col = 0; col < Props.Count; col++)
                Row.CreateCell(col).SetCellValue(Props.ElementAt(col).Name);

            var format = book.CreateDataFormat();

            foreach (var fil in Filas)
            {
                Row = Sheet.CreateRow(RowNum);
                RowNum++;

                for (var col = 0; col < Props.Count; col++)
                {
                    var Prop = Props[col];

                    var Val = Prop.ValGet(fil);

                    if (Val is null)
                        continue;

                    var Cell = Row.CreateCell(col);

                    if (Val is string cad)
                        Cell.SetCellValue(cad);
                    else if (Val is char chr)
                        Cell.SetCellValue(chr.ToString());
                    else if (Val is double num)
                        Cell.SetCellValue(num);
                    else if (Val is decimal dcm)
                        Cell.SetCellValue((double)dcm);
                    else if (Val is int ent)
                        Cell.SetCellValue(ent);
                    else if (Val is long lng)
                        Cell.SetCellValue(lng);
                    else if (Val is DateTime fec)
                        Cell.SetCellValue(fec);
                    else if (Val is bool bol)
                        Cell.SetCellValue(bol);

                    if (Prop.IsVal)
                    {
                        var Style = book.CreateCellStyle();
                        Style.DataFormat = format.GetFormat("_-\\$ * #,##0.00_-;-\\$ * #,##0.00_-;_-\\$ * \" - \"??_-;_-@_-");
                        Row.GetCell(col).CellStyle = Style;
                    }
                }
            }

            using (var file = new FileStream(FileName, FileMode.Create, FileAccess.Write))
            {
                book.Write(file, true);
                file.Close();
            }
        }
        private class Property<TRow>
        {
            internal string Name { get; set; }
            internal Func<TRow, object> ValGet { get; set; }
            internal bool IsVal { get; set; }
        }
    }
}