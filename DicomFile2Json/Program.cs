using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DicomFile2Json;
using JsonConverter;

namespace DicomFile2Json
{
    class Program
    {
        // A simple test driver. Feed any DICOM Part 10 file to it.
        static void Main(string[] args)
        {
            var s = DicomToJson.Convert(args[0]);
            Console.Write(s);
            Console.WriteLine("Done! Press any key.");
            Console.ReadKey();
        }
    }
}
