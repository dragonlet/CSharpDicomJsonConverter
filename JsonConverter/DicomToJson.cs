using System;
using System.Collections.Generic;
using System.Text;
using Dicom;
using Dicom.IO.Buffer;

namespace JsonConverter
{
    public class DicomToJson
    {
        [Flags]
        public enum OutputFormat
        {
            AddLinefeeds = 0x1,
            QIDOFormat = 0x2
        };

        public static String Convert(String fileName, OutputFormat format = OutputFormat.AddLinefeeds,
            int excludeElementsLargerThanBytes = 1024)
        {
            var sb = new StringBuilder();
            DicomFile file = DicomFile.Open(fileName);
            new DicomDatasetWalker(file.Dataset).Walk(new ToStringWalker(sb, format, excludeElementsLargerThanBytes));
            return sb.ToString();
        }

        public static String Convert(DicomDataset dataSet, OutputFormat format = OutputFormat.AddLinefeeds,
            int excludeElementsLargerThanBytes = 1024)
        {
            var sb = new StringBuilder();
            new DicomDatasetWalker(dataSet).Walk(new ToStringWalker(sb, format, excludeElementsLargerThanBytes));
            return sb.ToString();
        }

        private class ToStringWalker : IDicomDatasetWalker
        {
            private readonly String _crlf = "";
            private readonly Stack<int> _localElementCount = new Stack<int>();
            private readonly Stack<int> _localItemCount = new Stack<int>();
            private readonly int _maxBytes;
            private readonly Boolean _qidoBrace;
            private readonly StringBuilder _sb;
            private int _level;

            public ToStringWalker(StringBuilder sb, OutputFormat format, int maxElementSizeBytes = 1024)
            {
                _maxBytes = maxElementSizeBytes;
                if ((format & OutputFormat.AddLinefeeds) != 0) _crlf = "\r\n";
                if ((format & OutputFormat.QIDOFormat) != 0) _qidoBrace = true;
                _sb = sb;
            }

            private int Level
            {
                get { return _level; }
                set
                {
                    if (_crlf == "") return;
                    _level = value;
                    Indent = String.Empty;
                    for (int i = 0; i < _level; i++)
                        Indent += "   ";
                }
            }

            private string Indent { get; set; }

            public void OnBeginWalk(DicomDatasetWalker walker, DicomDatasetWalkerCallback callback)
            {
                if (_qidoBrace) _sb.AppendFormat("[{0}", _crlf);
                else
                {
                    _sb.Append("{");
                    _sb.Append(_crlf);
                }
                Level++;

                _localElementCount.Push(0);
                _localItemCount.Push(0);
            }

            public bool OnElement(DicomElement element)
            {
                var v = new StringBuilder();

                int eCount = _localElementCount.Pop();

                if (eCount > 0)
                {
                    v.AppendFormat(",{0}", _crlf);
                }

                eCount++;
                _localElementCount.Push(eCount);

                if (_qidoBrace)
                {
                    v.Append(Brace("{"));
                    v.Append(_crlf);
                }

                string tag = String.Format("{0:X04}{1:X04}", element.Tag.Group, element.Tag.Element);
                if (tag == "0040A30A")
                {
                }
                v.AppendFormat("{0}\"{1}\": ", Indent, tag);
                v.Append(Brace("{", false));
                v.Append(_crlf);
                string vr = element.ValueRepresentation.ToString();
                bool isString = element.ValueRepresentation.IsString;
                v.AppendFormat("{0}{1}:{2},", Indent, EnQuote("vr"), EnQuote(vr));
                v.Append(_crlf);
                v.AppendFormat("{0}{1}:", Indent, EnQuote("Values"));

                if (element.Length <= _maxBytes)
                {
                    v.Append(" [");
                    if (vr != "PN")
                    {
                        var vals = element.Get<string[]>();
                        int count = 0;

                        foreach (string ev in vals)
                        {
                            // String must be trimmed due to even-byte rules of DICOM and some vendors put \0 into the string
                            // which will cause an ill effect in JSON.
                            string ev2 = ev.Trim('\0');
                            if (count > 0) v.Append(", ");
                            v.Append(isString ? EnQuote(ev2) : ev2);
                            count++;
                        }
                    }
                    else
                    {
                        bool multiNameField = false;

                        var vals = element.Get<string[]>();

                        v.Append(_crlf);

                        foreach (string ev in vals)
                        {
                            string ev2 = ev.Trim('\0');
                            if (multiNameField)
                            {
                                v.Append(", ");
                                v.Append(_crlf);
                                v.Append(Indent);
                            }

                            v.Append(Brace("{"));
                            v.Append(_crlf);
                            Level++;
                            v.Append(Indent);
                            v.AppendFormat("{0}: {1}", EnQuote("Alphabetic"), EnQuote(ev2));
                            Level--;
                            v.Append(_crlf);
                            v.Append(Brace("}"));

                            multiNameField = true;
                        }
                    }
                }
                else
                {
                    v.Append("[");
                }
                v.Append("]");
                v.Append(_crlf);

                if (_qidoBrace)
                {
                    v.Append(Brace("}"));
                    v.Append(_crlf);
                }

                v.Append(Brace("}"));
                _sb.Append(v);
                return true;
            }

            public bool OnBeginSequence(DicomSequence sequence)
            {
                var v = new StringBuilder();

                int eCount = _localElementCount.Pop();

                if (eCount > 0)
                {
                    v.AppendFormat(",{0}", _crlf);
                }
                else
                {
                    v.Append(_crlf);
                }

                _localElementCount.Push(++eCount);
                _localElementCount.Push(0);
                _localItemCount.Push(0);

                string tag = String.Format("{0:X04}{1:X04}", sequence.Tag.Group, sequence.Tag.Element);
                v.AppendFormat("{0}\"{1}\": ", Indent, tag);
                v.Append(Brace("{", false));
                v.Append(_crlf);
                // vr = ""
                v.AppendFormat("{0}{1}:{2},", Indent, EnQuote("vr"), EnQuote(sequence.ValueRepresentation.ToString()));
                v.Append(_crlf);
                v.AppendFormat("{0}{1}:", Indent, EnQuote("Values"));

                v.Append(" [");
                v.Append(_crlf);
                _sb.Append(v);
                return true;
            }

            public bool OnBeginSequenceItem(DicomDataset dataset)
            {
                int iCount = _localItemCount.Pop();
                if (iCount > 0)
                    _sb.AppendFormat(",{0}", _crlf);
                _localItemCount.Push(++iCount);
                _localElementCount.Push(0);
                _sb.Append(Brace("{"));
                _sb.Append(_crlf);
                return true;
            }

            public bool OnEndSequenceItem()
            {
                _localElementCount.Pop();
                _sb.AppendFormat("{0}", _crlf);
                _sb.Append(Brace("}"));
                return true;
            }

            public bool OnEndSequence()
            {
                var v = new StringBuilder();
                v.Append("]");
                v.Append(_crlf);
                v.Append(Brace("}"));
                _sb.Append(v);
                _localElementCount.Pop();
                _localItemCount.Pop();
                return true;
            }

            public bool OnBeginFragment(DicomFragmentSequence fragment)
            {
                //var tag = String.Format("{0}{1}  {2}", Indent, fragment.Tag.ToString().ToUpper(), fragment.Tag.DictionaryEntry.Name);

                //Form.AddItem(tag, fragment.ValueRepresentation.Code, String.Empty, String.Empty);

                Level++;
                return true;
            }

            public bool OnFragmentItem(IByteBuffer item)
            {
                //var tag = String.Format("{0}Fragment", Indent);

                //Form.AddItem(tag, String.Empty, item.Size.ToString(), String.Empty);
                return true;
            }

            public bool OnEndFragment()
            {
                Level--;
                return true;
            }

            public void OnEndWalk()
            {
                if (_qidoBrace)
                {
                    _sb.AppendFormat("]{0}", _crlf);
                }
                else
                {
                    _sb.Append(_crlf);
                    _sb.Append("}");
                    _sb.Append(_crlf);
                }
            }

            private static string EnQuote(String v)
            {
                return "\"" + v.Replace("\"", "&quot;") + "\"";
            }

            public override string ToString()
            {
                return _sb.ToString();
            }

            private String Brace(String brace, bool indent = true)
            {
                if (brace == "{")
                {
                    Level++;
                    if (indent)
                    {
                        return Indent + "{";
                    }
                }
                if (brace == "}")
                {
                    string rv = Indent + "}";
                    Level--;
                    if (indent)
                    {
                        return rv;
                    }
                }
                return brace;
            }
        }
    }
}