using Elva.MVVM.ViewModel.Model;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Navigation;
using iText.Layout;
using iText.Layout.Element;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Elva.MVVM.Model.Export
{
    class PDFExport
    {
        private ComicVM _comicVM;
        private IProgress<int>? _progress;
        private string _filePath;
        private string _comicDirectory;
        private string _imageDirectory;
        public PDFExport(ComicVM comicVM, IProgress<int> progress)
        {
            _progress = progress;
            _comicVM = comicVM;
            _comicDirectory = comicVM.GetComicDestination();
            _imageDirectory = System.IO.Path.Combine(_comicDirectory, "Images\\");
            _filePath = System.IO.Path.Combine(_comicDirectory, System.IO.Path.GetFileName(_comicDirectory) + ".pdf");
            Debug.WriteLine(_filePath);
        }

        public void CreatePDF()
        {
            _progress?.Report(1);

            Debug.WriteLine("Get File Info");
            List<ExportChapter> chapters = new();
            chapters.Add(new ExportChapter("Cover", 0));
            chapters.AddRange(_comicVM.ChapterVMs.Where(x => x.DownloadProgress == 100).Select(x => new ExportChapter($"Chapter: {x.Number}{(!string.IsNullOrEmpty(x.Title) ? " - " + x.Title : "")}", x.Order)).ToList());

            try { chapters[0].AddImage(new Image(ImageDataFactory.Create(_comicVM.CoverPath))); } catch { }
            float chapterPercentage = 20 / chapters.Count;
            float percentage = 0;

            Parallel.ForEach(chapters[1..], new ParallelOptions() { MaxDegreeOfParallelism = 3 }, chapter =>
            {
                string[] fileInfos = new DirectoryInfo(_imageDirectory).GetFiles($"{chapter.Order}_*").Select(x => x.FullName).ToArray();
                Array.Sort(fileInfos, new ExplorerComparer());
                Array.ForEach(fileInfos, x => { try { chapter.AddImage(new Image(ImageDataFactory.Create(x))); } catch { } });
                percentage += chapterPercentage;
                _progress?.Report((int)percentage);
            });
            _progress?.Report(20);

            if (File.Exists(_filePath))
                File.Delete(_filePath);
            Debug.WriteLine("Create PDF");

            using FileStream fs = new(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using PdfWriter writer = new(fs, new WriterProperties().AddXmpMetadata().SetPdfVersion(PdfVersion.PDF_2_0));
            PdfDocument pdf = new(writer);
            PdfDocumentInfo info = pdf.GetDocumentInfo();
            info.SetTitle(_comicVM.Title);
            info.SetAuthor(_comicVM.Author);
            info.SetSubject("Comic");
            info.SetKeywords($"Elva, {_comicVM.Title}");
            info.SetCreator("Elva");
            Document doc = new(pdf);
            doc.SetMargins(0, 0, 0, 0); PdfOutline topOutline = pdf.GetOutlines(false);
            int allImages = chapters.Sum(x => x.ImageCounter);
            _progress?.Report(25);
            percentage = 25;
            chapterPercentage = 70 / chapters.Count;

            foreach (ExportChapter chapter in chapters)
            {
                PdfOutline outline = topOutline.AddOutline(chapter.Title);
                outline.AddDestination(PdfDestination.MakeDestination(new PdfString(chapter.Title)));
                bool first = true;
                foreach (Image image in chapter.ContentImages)
                {
                    if (image.GetImageWidth() * 1.55f > 14350)
                        return;
                    else if (image.GetImageWidth() * 1.8f > image.GetImageHeight()) //1.55
                        pdf.SetDefaultPageSize(new PageSize(image.GetImageWidth(), image.GetImageHeight()));
                    else
                        for (int j = 0; 0 < (image.GetImageHeight() - (j * image.GetImageWidth() * 1.45f)); j++)
                            if ((image.GetImageHeight() - (j * image.GetImageWidth() * 1.45f)) >= image.GetImageWidth() * 1.45f)
                            {
                                pdf.SetDefaultPageSize(new PageSize(image.GetImageWidth(), image.GetImageWidth() * 1.45f));
                                image.SetFixedPosition(0, -image.GetImageHeight() + ((1 + j) * image.GetImageWidth() * 1.45f));
                            }
                            else
                                pdf.SetDefaultPageSize(new PageSize(image.GetImageWidth(), image.GetImageHeight() - (j * image.GetImageWidth() * 1.45f)));

                    doc.Add(first ? image.SetDestination(chapter.Title) : image);
                    first = false;
                }
                percentage += chapterPercentage;
                _progress?.Report((int)percentage);
            }
            _progress?.Report(100);
            doc.Close();
        }
    }
}
