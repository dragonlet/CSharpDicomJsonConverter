using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

using Dicom;
using Dicom.IO;
using Dicom.IO.Buffer;

namespace JsonConverter
{
    public class DicomToJson
    {
        static public String Convert(String fileName, Boolean addCrLf = true,
            int excludeElementsLargerThanBytes = 1024)
        {
            var sb = new StringBuilder();
			DicomFile file = DicomFile.Open(fileName);
	        new DicomDatasetWalker(file.Dataset).Walk(new ToStringWalker(sb, true, 1024));
            return sb.ToString();
        }
        
        private class ToStringWalker : IDicomDatasetWalker
        {
            private readonly int _maxBytes = 0;
            private StringBuilder _sb;
            private int _level;
            private readonly String _crlf = "";
            private int _totalEmements = 0;

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

            private string Indent
            {
                get;
                set;
            }

            private string quot(String v)
            {
                return "\"" + v + "\"";
            }

            public ToStringWalker(StringBuilder sb, Boolean addCrLf = true, int maxElementSizeBytes = 1024)
            {
                _maxBytes = maxElementSizeBytes;
                if (addCrLf) _crlf = "\r\n";
                _sb = sb;
            }

            public void OnBeginWalk(DicomDatasetWalker walker, DicomDatasetWalkerCallback callback)
            {
                _sb.AppendFormat("[{0}",_crlf);
                Level++;
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
                    var rv = Indent + "}";
                    Level--;
                    if (indent)
                    {
                        return rv;
                    }
                }
                return brace;
            }

            public bool OnElement(DicomElement element)
            {
                var v = new StringBuilder();
                if (_totalEmements > 0)
                {
                    v.AppendFormat(",{0}", _crlf);
                }
                _totalEmements++;
                v.Append(Brace("{"));
                v.Append(_crlf);
                var tag = String.Format("{0:X04}{1:X04}", element.Tag.Group, element.Tag.Element);
                v.AppendFormat("{0}\"{1}\": ", Indent, tag);
                v.Append(Brace("{", false));
                v.Append(_crlf);
                var vr = element.ValueRepresentation.ToString();
                var isString = element.ValueRepresentation.IsString;
                v.AppendFormat("{0}{1}:{2},", Indent, quot("vr"), quot(vr));
                v.Append(_crlf);
                v.AppendFormat("{0}{1}:", Indent, quot("Values"));
 
                if (element.Length <= _maxBytes)
                {
                    v.Append(" [");
                    if (vr != "PN")
                    {
                        var vals = element.Get<string[]>();
                        var count = 0;

                        foreach (var ev in vals)
                        {
                            if (count > 0) v.Append(", ");
                            if (isString)
                            {
                                v.Append(quot(ev));
                            }
                            else
                            {
                                v.Append(ev);
                            }
                            count++;
                        }   
                    }
                    else
                    {
                        bool multiNameField = false;

                        var vals = element.Get<string[]>();
                        var count = 0;

                        v.Append(_crlf);

                        foreach (var ev in vals)
                        {
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
                            v.AppendFormat("{0}: {1}", quot("Alphabetic"), quot(ev));
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
                v.Append(Brace("}"));
                v.Append(_crlf);
                v.Append(Brace("}")); _sb.Append(v.ToString());
                return true;
            }

            public bool OnBeginSequence(DicomSequence sequence)
            {
                var v = new StringBuilder();
                v.AppendFormat("{0}", _crlf);
                v.Append(Brace("{"));
                v.Append(_crlf);
                var tag = String.Format("{0:X04}{1:X04}", sequence.Tag.Group, sequence.Tag.Element);
                v.AppendFormat("{0}\"{1}\": ", Indent, tag);
                v.Append(Brace("{", false));
                v.Append(_crlf);
                // vr = ""
                v.AppendFormat("{0}{1}:{2},", Indent, quot("vr"), quot(sequence.ValueRepresentation.ToString()));
                v.Append(_crlf);
                v.AppendFormat("{0}{1}:", Indent, quot("Values"));

                v.Append(" [");
                v.Append(_crlf);
                _sb.Append(v.ToString());
                return true;
            }

            public bool OnBeginSequenceItem(DicomDataset dataset)
            {
                return true;
            }

            public bool OnEndSequenceItem()
            {
                return true;
            }

            public bool OnEndSequence()
            {
                var v = new StringBuilder();
                v.Append("]");
                v.Append(_crlf);
                v.Append(Brace("}"));
                v.Append(_crlf);
                v.Append(Brace("}")); 
                _sb.Append(v.ToString());
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
                _sb.AppendFormat("{0}]{1}", _crlf,  _crlf);
            }
            
        }

    }
}
