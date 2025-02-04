using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebTrigger.Model;
using CsvHelper.TypeConversion;
using DateTimeConverter = CsvHelper.TypeConversion.DateTimeConverter;
namespace WebTrigger.Service
{
    public static class CSVService<T>
    {
        public static IEnumerable<T> ReadCSV(string data)
        {
            using var reader = new StringReader(data);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null
            });

            csv.Context.TypeConverterCache.AddConverter<DateTime>(new DateTimeConverter());
            var result = csv.GetRecords<T>().ToList();
            return result.AsEnumerable();
        }
    }
}
