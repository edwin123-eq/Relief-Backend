using Microsoft.VisualBasic;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;

public static class CNV
{
    public static bool UseAbsValueInFormatRate { get; set; } = false;
    public static bool UseAbsValueInFormatQty { get; set; } = false;

    public static string FormatRate(object val, bool absVal)
    {
        if (!decimal.TryParse(val?.ToString() ?? "0", out decimal result))
            result = 0;

        return (absVal ? Math.Abs(result) : result).ToString("0.00");
    }

    public static string FormatRate(object val) => FormatRate(val, UseAbsValueInFormatRate);

    public static string FormatQuantity(object val, bool absVal)
    {
        if (!decimal.TryParse(val?.ToString() ?? "0", out decimal result))
            result = 0;

        return (absVal ? Math.Abs(result) : result).ToString("0.000");
    }

    public static string FormatQuantity(object val) => FormatQuantity(val, UseAbsValueInFormatQty);

    public static string FormatQuantityND(object val, bool absVal)
    {
        if (!decimal.TryParse(val?.ToString() ?? "0", out decimal result))
            result = 0;

        return (absVal ? Math.Abs(result) : result).ToString("0");
    }

    public static string FormatQuantityND(object val) => FormatQuantityND(val, UseAbsValueInFormatQty);

    public static string FormatRateExtended(object val, bool absVal)
    {
        if (!decimal.TryParse(val?.ToString() ?? "0", out decimal result))
            result = 0;

        return (absVal ? Math.Abs(result) : result).ToString("0.00000");
    }

    public static string FormatRateExtended(object val) => FormatRateExtended(val, UseAbsValueInFormatRate);

    public static long ToLong(object val) => long.TryParse(val?.ToString() ?? "0", out long result) ? result : 0;

    public static int ToInt(object val) => int.TryParse(val?.ToString() ?? "0", out int result) ? result : 0;

    public static double ToDouble(object val) => double.TryParse(val?.ToString() ?? "0", out double result) ? result : 0.0;

    public static decimal ToDecimal(object val) => decimal.TryParse(val?.ToString() ?? "0", out decimal result) ? result : 0m;

    public static DateTime ToDate(object val)
    {
        DateTime defaultDate = new DateTime(599266080000000000L);
        return DateTime.TryParse(val?.ToString(), out DateTime result) && result >= defaultDate ? result : defaultDate;
    }
    public static object ReplaceNull(object val, object replaceVal)
    {
        if (val == null || Convert.IsDBNull(val))
        {
            return replaceVal;
        }

        return val;
    }

    //public static Image ImageFromByte(byte[] b)
    //{
    //    using MemoryStream stream = new MemoryStream(b);
    //    return Image.FromStream(stream);
    //}

    //public static byte[] ImageToByte(Image img)
    //{
    //    using MemoryStream memoryStream = new MemoryStream();
    //    img.Save(memoryStream, ImageFormat.Bmp);
    //    return memoryStream.ToArray();
    //}

    //public static Image GetBlankImage()
    //{
    //    Bitmap bitmap = new Bitmap(100, 100);
    //    using Graphics g = Graphics.FromImage(bitmap);
    //    g.FillRectangle(Brushes.White, 0, 0, 100, 100);
    //    return bitmap;
    //}

    public static string ObjectToXML(object x)
    {
        try
        {
            using StringWriter stringWriter = new StringWriter();
            XmlSerializer xmlSerializer = new XmlSerializer(x.GetType());
            xmlSerializer.Serialize(stringWriter, x);
            return stringWriter.ToString();
        }
        catch
        {
            return "";
        }
    }

    public static object XMLToObject(string xmlStr, Type type)
    {
        try
        {
            using StringReader textReader = new StringReader(xmlStr);
            XmlSerializer xmlSerializer = new XmlSerializer(type);
            return xmlSerializer.Deserialize(textReader);
        }
        catch
        {
            return null;
        }
    }
}
