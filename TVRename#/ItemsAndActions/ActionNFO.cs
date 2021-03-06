﻿// 
// Main website for TVRename is http://tvrename.com
// 
// Source code available at http://code.google.com/p/tvrename/
// 
// This code is released under GPLv3 http://www.gnu.org/licenses/gpl.html
// 
namespace TVRename
{
    using System;
    using System.IO;
    using System.Windows.Forms;
    using System.Xml;

    public class ActionNFO : Item, Action, ScanListItem, ActionWriteMetadata
    {
        public ShowItem SI; // if for an entire show, rather than specific episode
        public FileInfo Where;

        public ActionNFO(FileInfo nfo, ProcessedEpisode pe)
        {
            this.SI = null;
            this.Episode = pe;
            this.Where = nfo;
        }

        public ActionNFO(FileInfo nfo, ShowItem si)
        {
            this.SI = si;
            this.Episode = null;
            this.Where = nfo;
        }

        #region Action Members

        public string Name
        {
            get { return "Write XBMC Metadata"; }
        }

        public bool Done { get; private set; }
        public bool Error { get; private set; }
        public string ErrorText { get; set; }

        public string ProgressText
        {
            get { return this.Where.Name; }
        }

        public double PercentDone
        {
            get { return this.Done ? 100 : 0; }
        }

        public long SizeOfWork
        {
            get { return 10000; }
        }

        public string produces
        {
            get { return this.Where.FullName; }
        }


        public bool Go(ref bool pause, TVRenameStats stats)
        {
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                NewLineOnAttributes = true
            };
            // "try" and silently fail.  eg. when file is use by other...
            XmlWriter writer;
            try
            {
                //                XmlWriter writer = XmlWriter.Create(this.Where.FullName, settings);
                writer = XmlWriter.Create(this.Where.FullName, settings);
                if (writer == null)
                    return false;
            }
            catch (Exception)
            {
                this.Done = true;
                return true;
            }

            if (this.Episode != null) // specific episode
            {
                // See: http://xbmc.org/wiki/?title=Import_-_Export_Library#TV_Episodes
                writer.WriteStartElement("episodedetails");

                XMLHelper.WriteElementToXML(writer,"title",this.Episode.Name);
                XMLHelper.WriteElementToXML(writer,"rating",this.Episode.EpisodeRating);
                XMLHelper.WriteElementToXML(writer,"season",this.Episode.SeasonNumber);
                XMLHelper.WriteElementToXML(writer,"episode",this.Episode.EpNum);
                XMLHelper.WriteElementToXML(writer,"plot",this.Episode.Overview);

                writer.WriteStartElement("aired");
                if (this.Episode.FirstAired != null)
                    writer.WriteValue(this.Episode.FirstAired.Value.ToString("yyyy-MM-dd"));
                writer.WriteEndElement();

                if (this.Episode.SI != null)
                {
                    WriteInfo(writer, this.Episode.SI, "ContentRating", "mpaa");
                }

                //Director(s)
                if (!String.IsNullOrEmpty(this.Episode.EpisodeDirector))
                {
                    string EpDirector = this.Episode.EpisodeDirector;
                    if (!string.IsNullOrEmpty(EpDirector))
                    {
                        foreach (string Daa in EpDirector.Split('|'))
                        {
                            if (string.IsNullOrEmpty(Daa))
                                continue;

                            XMLHelper.WriteElementToXML(writer,"director",Daa);
                        }
                    }
                }

                //Writers(s)
                if (!String.IsNullOrEmpty(this.Episode.Writer))
                {
                    string EpWriter = this.Episode.Writer;
                    if (!string.IsNullOrEmpty(EpWriter))
                    {
                        XMLHelper.WriteElementToXML(writer,"credits",EpWriter);
                    }
                }

                // Guest Stars...
                if (!String.IsNullOrEmpty(this.Episode.EpisodeGuestStars))
                {
                    string RecurringActors = "";

                    if (this.Episode.SI != null)
                    {
                        RecurringActors = this.Episode.SI.TheSeries().GetItem("Actors");
                    }

                    string GuestActors = this.Episode.EpisodeGuestStars;
                    if (!string.IsNullOrEmpty(GuestActors))
                    {
                        foreach (string Gaa in GuestActors.Split('|'))
                        {
                            if (string.IsNullOrEmpty(Gaa))
                                continue;

                            // Skip if the guest actor is also in the overal recurring list
                            if (!string.IsNullOrEmpty(RecurringActors) && RecurringActors.Contains(Gaa))
                            {
                                continue;
                            }

                            writer.WriteStartElement("actor");
                            XMLHelper.WriteElementToXML(writer,"name",Gaa);
                            writer.WriteEndElement(); // actor
                        }
                    }
                }

                // actors...
                if (this.Episode.SI != null)
                {
                    string actors = this.Episode.SI.TheSeries().GetItem("Actors");
                    if (!string.IsNullOrEmpty(actors))
                    {
                        foreach (string aa in actors.Split('|'))
                        {
                            if (string.IsNullOrEmpty(aa))
                                continue;

                            writer.WriteStartElement("actor");
                            XMLHelper.WriteElementToXML(writer,"name",aa);
                            writer.WriteEndElement(); // actor
                        }
                    }
                }

                writer.WriteEndElement(); // episodedetails
            }
            else if (this.SI != null) // show overview (tvshow.nfo)
            {
                // http://www.xbmc.org/wiki/?title=Import_-_Export_Library#TV_Shows

                writer.WriteStartElement("tvshow");

                XMLHelper.WriteElementToXML(writer,"title",this.SI.ShowName);

                XMLHelper.WriteElementToXML(writer, "episodeguideurl", TheTVDB.BuildURL(true, true, this.SI.TVDBCode, TheTVDB.Instance.RequestLanguage));

                WriteInfo(writer, this.SI, "Overview", "plot");

                string genre = this.SI.TheSeries().GetItem("Genre");
                if (!string.IsNullOrEmpty(genre))
                {
                    genre = genre.Trim('|');
                    genre = genre.Replace("|", " / ");
                    XMLHelper.WriteElementToXML(writer,"genre",genre);
                }

                WriteInfo(writer, this.SI, "FirstAired", "premiered");
                WriteInfo(writer, this.SI, "Year", "year");
                WriteInfo(writer, this.SI, "Rating", "rating");
                WriteInfo(writer, this.SI, "Status", "status");

                // actors...
                string actors = this.SI.TheSeries().GetItem("Actors");
                if (!string.IsNullOrEmpty(actors))
                {
                    foreach (string aa in actors.Split('|'))
                    {
                        if (string.IsNullOrEmpty(aa))
                            continue;

                        writer.WriteStartElement("actor");
                        XMLHelper.WriteElementToXML(writer,"name",aa);
                        writer.WriteEndElement(); // actor
                    }
                }

                WriteInfo(writer, this.SI, "ContentRating", "mpaa");
                WriteInfo(writer, this.SI, "IMDB_ID", "id", "moviedb","imdb");

                XMLHelper.WriteElementToXML(writer,"tvdbid",this.SI.TheSeries().TVDBCode);

                string rt = this.SI.TheSeries().GetItem("Runtime");
                if (!string.IsNullOrEmpty(rt))
                {
                    XMLHelper.WriteElementToXML(writer,"runtime",rt + " minutes");
                }

                writer.WriteEndElement(); // tvshow
            }

            try
            {
                writer.Close();
            }
            catch (Exception e)
            {
                this.ErrorText = e.Message;
                this.Error = true;
                this.Done = true;
                return false;     
            }

            this.Done = true;
            return true;
        }

        #endregion

        #region Item Members

        public bool SameAs(Item o)
        {
            return (o is ActionNFO) && ((o as ActionNFO).Where == this.Where);
        }

        public int Compare(Item o)
        {
            ActionNFO nfo = o as ActionNFO;

            if (this.Episode == null)
                return 1;
            if (nfo == null || nfo.Episode == null)
                return -1;
            return (this.Where.FullName + this.Episode.Name).CompareTo(nfo.Where.FullName + nfo.Episode.Name);
        }

        #endregion

        #region ScanListItem Members

        public IgnoreItem Ignore
        {
            get
            {
                if (this.Where == null)
                    return null;
                return new IgnoreItem(this.Where.FullName);
            }
        }

        public ListViewItem ScanListViewItem
        {
            get
            {
                ListViewItem lvi = new ListViewItem();

                if (this.Episode != null)
                {
                    lvi.Text = this.Episode.SI.ShowName;
                    lvi.SubItems.Add(this.Episode.SeasonNumber.ToString());
                    lvi.SubItems.Add(this.Episode.NumsAsString());
                    DateTime? dt = this.Episode.GetAirDateDT(true);
                    if ((dt != null) && (dt.Value.CompareTo(DateTime.MaxValue)) != 0)
                        lvi.SubItems.Add(dt.Value.ToShortDateString());
                    else
                        lvi.SubItems.Add("");
                }
                else
                {
                    lvi.Text = this.SI.ShowName;
                    lvi.SubItems.Add("");
                    lvi.SubItems.Add("");
                    lvi.SubItems.Add("");
                }

                lvi.SubItems.Add(this.Where.DirectoryName);
                lvi.SubItems.Add(this.Where.Name);

                lvi.Tag = this;

                //lv->Items->Add(lvi);
                return lvi;
            }
        }

        string ScanListItem.TargetFolder
        {
            get
            {
                if (this.Where == null)
                    return null;
                return this.Where.DirectoryName;
            }
        }

        public string ScanListViewGroup
        {
            get { return "lvgActionMeta"; }
        }

        public int IconNumber
        {
            get { return 7; }
        }

        public ProcessedEpisode Episode { get; private set; }

        #endregion

        private static void WriteInfo(XmlWriter writer, ShowItem si, string whichItem, string elemName)
        {
            WriteInfo(writer, si, whichItem, elemName, null, null);
        }

        private static void WriteInfo(XmlWriter writer, ShowItem si, string whichItem, string elemName, string attribute, string attributeVal)
        {
            string t = si.TheSeries().GetItem(whichItem);
            if (!string.IsNullOrEmpty(t))
            {
                writer.WriteStartElement(elemName);
                if (!String.IsNullOrEmpty(attribute) && !String.IsNullOrEmpty(attributeVal))
                {
                    writer.WriteAttributeString(attribute, attributeVal);
                }
                writer.WriteValue(t);
                writer.WriteEndElement();
            }
        }
    }
}