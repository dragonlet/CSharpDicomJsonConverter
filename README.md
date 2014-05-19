CSharpDicomJsonConverter
========================

Dicom to Json Converter written in C# using fo-dicom

This contains both the class library and a simple command line utility to test-drive the class. The project is simple enough that you can get started using it right away.

The output should meet the QIDO-RS SearchForStudies response, which is described at ftp://medical.nema.org/medical/dicom/2013/output/chtml/part18/sect_F.4.html

I request that if you find a bug, issues or suggestions, please file a report on this repo.

To Build:

Download fo-dicom from https://github.com/rcd/fo-dicom

Place the fo-dicom and the CSharpDicomJsonConverter directories are under the root.
         
Doing so should allow the Visual Studio (2010) solution to load fo-dicom and this project without a need to change the project layout.

