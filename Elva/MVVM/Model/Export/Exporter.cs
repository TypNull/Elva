using Elva.MVVM.ViewModel.Model;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Navigation;
using iText.Layout;
using iText.Layout.Element;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Elva.MVVM.Model.Export
{
    public enum ExportFormat
    {
        PDF,
        ZIP,
        CBZ
    }

    internal static class Exporter
    {

        public static void CreateExport(ComicVM comicVM, ExportFormat format, IProgress<byte> progress, int[]? includedChapter = null)
        {
            progress?.Report(1);
            string comicDirectory = comicVM.GetComicDestination();
            string imageDirectory = System.IO.Path.Combine(comicDirectory, "Images\\");
            string filePath = System.IO.Path.Combine(comicDirectory, System.IO.Path.GetFileName(comicDirectory) + "." + format.ToString().ToLower());

            Debug.WriteLine("Get comic file info");

            List<ExportChapter> chapters = LoadChapters(comicVM, progress!, imageDirectory, includedChapter);
            progress?.Report(10);
            if (File.Exists(filePath))
                File.Delete(filePath);

            try
            {
                switch (format)
                {
                    case ExportFormat.PDF:
                        ExportAsPDF(comicVM, progress!, filePath, chapters);
                        break;
                    case ExportFormat.ZIP:
                    case ExportFormat.CBZ:
                        ExportAsZip(progress!, filePath, chapters);
                        break;
                    default:
                        break;
                }
                progress?.Report(100);
            }
            catch (Exception)
            {

            }

        }

        private static List<ExportChapter> LoadChapters(ComicVM comicVM, IProgress<byte> progress, string imageDirectory, int[]? includedChapter = null)
        {
            List<ExportChapter> chapters = [new ExportChapter("Cover", 0)];
            if (includedChapter == null)
                chapters.AddRange(comicVM.ChapterVMs.Where(x => x.DownloadProgress == 100).Select(x => new ExportChapter($"Chapter: {x.Number}{(!string.IsNullOrEmpty(x.Title) ? " - " + x.Title : "")}", x.Order)).ToList());
            else
                chapters.AddRange(comicVM.ChapterVMs.Where(x => includedChapter.Contains(x.Order) && x.DownloadProgress == 100).Select(x => new ExportChapter($"Chapter: {x.Number}{(!string.IsNullOrEmpty(x.Title) ? " - " + x.Title : "")}", x.Order)).ToList());

            if (!string.IsNullOrEmpty(comicVM.CoverPath))
                chapters[0].AddImage(comicVM.CoverPath);

            float chapterPercentage = 10 / chapters.Count;
            float percentage = 0;

            foreach (ExportChapter chapter in chapters[1..])
            {
                string[] fileInfos = new DirectoryInfo(imageDirectory).GetFiles($"{chapter.Order}_*").Select(x => x.FullName).ToArray();
                Array.Sort(fileInfos, new ExplorerComparer());
                Array.ForEach(fileInfos, chapter.AddImage);
                percentage += chapterPercentage;
                progress?.Report((byte)percentage);
            }

            return chapters;
        }

        private static void ExportAsZip(IProgress<byte> progress, string filePath, List<ExportChapter> exportChapters)
        {
            Debug.WriteLine("Create Zip");
            float chapterPercentage = 89 / exportChapters.Count;
            float percentage = 10;
            using (FileStream fileStream = new(filePath, FileMode.Create))
            {
                using (ZipArchive archive = new(fileStream, ZipArchiveMode.Update, true))
                {
                    foreach (ExportChapter chapter in exportChapters)
                    {
                        foreach (string image in chapter.ContentImages)
                        {
                            try
                            {
                                archive.CreateEntryFromFile(image, System.IO.Path.GetFileName(image));
                            }
                            catch (Exception) { }
                        }
                        percentage += chapterPercentage;
                        progress?.Report((byte)percentage);
                    }
                }
            }
            Debug.WriteLine(filePath);
            Debug.WriteLine("Zip file created");
        }


        private static void ExportAsPDF(ComicVM comicVM, IProgress<byte> progress, string filePath, List<ExportChapter> exportChapters)
        {
            Debug.WriteLine("Create PDF");
            using FileStream fs = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using PdfWriter writer = new(fs, new WriterProperties().AddXmpMetadata().SetPdfVersion(PdfVersion.PDF_2_0));
            PdfDocument pdf = new(writer);
            PdfDocumentInfo info = pdf.GetDocumentInfo();
            info.SetTitle(comicVM.Title);
            info.SetAuthor(comicVM.Author);
            info.SetSubject("Comic");
            info.SetKeywords($"Elva, {comicVM.Title}");
            info.SetCreator("Elva");
            Document doc = new(pdf);
            doc.SetMargins(0, 0, 0, 0); PdfOutline topOutline = pdf.GetOutlines(false);

            progress?.Report(13);
            float chapterPercentage = 85 / exportChapters.Count;
            float percentage = 13;

            foreach (ExportChapter chapter in exportChapters)
            {
                PdfOutline outline = topOutline.AddOutline(chapter.Title);
                outline.AddDestination(PdfDestination.MakeDestination(new PdfString(chapter.Title)));
                bool first = true;
                foreach (Image image in chapter.CreateITextImages())
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
                progress?.Report((byte)percentage);
            }
            doc.Close();
            Debug.WriteLine("PDF creation finished");
        }
    }
}
