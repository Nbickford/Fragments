using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NAudio;
using NAudio.Wave;

namespace Analytical {
    class Program {

        static List<String> filenames;
        static List<float[]> allBeats;
        static List<AnalysedSong> analysedSongs;

        static double[][] sinconvolvematrix;
        static double[][] cosconvolvematrix;

        static void Main(string[] args) {
            //Generate waves for use in convolution.
            sinconvolvematrix = new double[60][];
            cosconvolvematrix = new double[60][];
            for (int i = 0; i < 60; i++) {
                float basefreq = 27.5f;
                double f = 2 * Math.PI * Math.Pow(2, i / 12.0f) * basefreq;
                sinconvolvematrix[i] = new double[44100];
                cosconvolvematrix[i] = new double[44100];
                for (int j = 0; j < 44100; j++) {
                    sinconvolvematrix[i][j] = Math.Sin(f * j / 44100);
                    cosconvolvematrix[i][j] = Math.Cos(f * j / 44100);
                }
            }

            //for a sine wave, RMS is a/Sqrt[2] over any multiple of period.
            ClearBeats();
            ReadBeats(@"C:\Users\neil\Documents\Sync\code\analytical\sonic-annotator-0.7-win32\mozart.csv");

            //The main loop: For each beat of every song, we compute the components of its Fourier transform
            //corresponding to the notes in a standard 12-tone scale.
            //In other words, for each beat we figure out what notes are playing and how loudly.
            analysedSongs = new List<AnalysedSong>();
            for(int filenum=0;filenum<filenames.Count;filenum++){
                Console.WriteLine(filenames[filenum]);

                //Initialize the AudioReader:
                AudioFileReader reader;
                try {
                    reader = new AudioFileReader(filenames[filenum].Replace('/', '\\'));
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                    continue;
                }

                Console.WriteLine(DateTime.Now);
                float meansquare = 0; //We also compute the rms of each beat to normalize things.
                float comp = 0;
                int totalSamples = 0;
                float[,] analysedbeats = new float[allBeats[filenum].Length, 60];

                float max = 0;

                for (int i = 0; i < allBeats[filenum].Length - 1; i++) {
                    float[] audio;
                    try {
                        audio = GetAudio(reader, allBeats[filenum][i], allBeats[filenum][i + 1]);
                    } catch (Exception ex) {
                        Console.WriteLine(ex);
                        continue; //fill in 0s for this beat.
                    }

                    //Compute the RMS of the beat using Kahan summation!
                    for (int j = 0; j < audio.Length; j++) {
                        float y = (audio[j] * audio[j]) - comp;
                        float t = meansquare + y;
                        comp = (t - meansquare) - y;
                        meansquare = t;
                    }
                    totalSamples += audio.Length;

                    double[] analysis = AnalyseAudio(audio, reader.WaveFormat.SampleRate);
                    for (int j = 0; j < 60; j++) {
                        analysedbeats[i, j] = (float)analysis[j];
                        if (analysedbeats[i, j] > max) max = analysedbeats[i, j];
                    }
                    //Console.Write(i);
                }
                float rms = (float)Math.Sqrt(meansquare / totalSamples);

                for (int j = 0; j < 60; j++) {
                    for (int i = 0; i < allBeats[filenum].Length; i++) {
                        analysedbeats[i, j] = analysedbeats[i, j] / rms;
                    }
                }

                analysedSongs.Add(new AnalysedSong(filenames[filenum], allBeats[filenum], analysedbeats));
            }

            Console.WriteLine("Done! Finding best matches...");
            SortedList<float, String> bestMatches;
            bestMatches = new SortedList<float, string>(); //Contains scores for each song combination.

            //...and find song combinations:
            for (int song1 = 0; song1 < analysedSongs.Count-1; song1++) {
                Console.WriteLine("Progress: "+song1);
                for (int song2 = song1+1; song2 < analysedSongs.Count; song2++) {
                    int bestOffset = 0;
                    float val = CompareSongs(analysedSongs[song1], analysedSongs[song2],out bestOffset);

                    if (!bestMatches.ContainsKey(val)) {
                        //Write out the mashup's parameters and score:
                        bestMatches.Add(val,
                            analysedSongs[song1].name + " vs. " + analysedSongs[song2].name + "o: " + bestOffset + " l: " +
                            (analysedSongs[song1].beatHarmonics.GetLength(0) > analysedSongs[song2].beatHarmonics.GetLength(0) ? 1 : 2));
                    }
                }
            }
            
            Console.WriteLine(DateTime.Now);

            TextWriter tw = new StreamWriter("best-mozart-2.txt");
            foreach (KeyValuePair<float, String> kvp in bestMatches) {
                Console.WriteLine(kvp.Key + ": " + kvp.Value);
                tw.WriteLine(kvp.Key + ": " + kvp.Value);
            }
            tw.Close();

            Console.WriteLine();

            //Write out beat data to a composite file for future use:
            tw = new StreamWriter("alldata-mozart-2.txt");
            for (int i = 0; i < analysedSongs.Count; i++) {
                tw.WriteLine(analysedSongs[i].name);
                for (int f = 0; f < analysedSongs[i].beatHarmonics.GetLength(0); f++) {
                    for (int b = 0; b < analysedSongs[i].beatHarmonics.GetLength(1); b++) {
                        tw.Write(analysedSongs[i].beatHarmonics[f, b]+"\t");
                    }
                    tw.WriteLine();
                }
            }
            tw.Close();

            //and that's it!
            Console.WriteLine("Done! Press ENTER to exit...");
            Console.ReadLine();
        }

        static float[] GetAudio(AudioFileReader reader, float startPos, float endPos) {
            float[] buffer;
            int samplerate=reader.WaveFormat.SampleRate;
            int numchannels=reader.WaveFormat.Channels;
            if (reader.CurrentTime.TotalSeconds <= startPos) {
                //Move the reader to the exact sample:
                float correctDTime=(float)(startPos-reader.CurrentTime.TotalSeconds);
                int correctNumPoints=(int)(samplerate * numchannels * correctDTime);
                buffer = new float[correctNumPoints];
                reader.Read(buffer, 0, correctNumPoints);
            }
            //Console.WriteLine("corrected for {0} seconds", reader.CurrentTime.TotalSeconds - startPos);
            startPos = (float)reader.CurrentTime.TotalSeconds;

            float dtime = (endPos - startPos); //length of the sample
            int numPoints = (int)(samplerate * dtime) * numchannels; //number of samples in both channels

            buffer = new float[numPoints];
            reader.Read(buffer, 0, numPoints);

            //Demultiplex the sample input and mix it into a single channel:
            float[] demultiplexed = new float[buffer.Length / numchannels];
            for (int j = 0; j < numchannels; j++) {
                for (int i = 0; i < demultiplexed.Length; i++) {
                    demultiplexed[i] += buffer[numchannels * i + j] / numchannels;
                }
            }

            return demultiplexed;
        }

        static double[] AnalyseAudio(float[] audio, int sampleRate) {
            //returns the amplitude of each of the frequency bands
            //27.5->880, A0->A5 =60 notes
            double[] sines = new double[60];
            double[] cosines = new double[60];
            if (sampleRate == 44100) {
                for (int fi = 0; fi < 60; fi++) {
                    for(int i=0;i<audio.Length;i++){
                        int k=(i%44100);
                        sines[fi] += sinconvolvematrix[fi][k] * audio[i];
                        cosines[fi] += cosconvolvematrix[fi][k] * audio[i];
                    }
                }
            } else {
                float basefreq = 27.5f;
                Console.WriteLine("Audio sample rate isn't 44.1 kHz! This analysis will be slightly slower.");
                
                for (int fi = 0; fi < sines.Length; fi++) {
                    double f = 2 * Math.PI * Math.Pow(2, fi / 12.0f) * basefreq;
                    for (int i = 0; i < audio.Length; i++) {
                        sines[fi] += audio[i]*Math.Sin(f * i / sampleRate);
                        cosines[fi] += audio[i]*Math.Cos(f * i / sampleRate);
                    }
                }
            }

            //Compute magnitude of each band and store in cosines.
            //If every sample were 1, we would divide by the samplerate 
            //to get the average magnitude.
            for (int i = 0; i < sines.Length; i++) {
                cosines[i] = Math.Sqrt(sines[i] * sines[i] + cosines[i] * cosines[i])/audio.Length;
            }

            return cosines;
        }

        static void ClearBeats() {
            if (filenames != null) {
                filenames.Clear();
            } else {
                filenames = new List<string>();
            }
            if (allBeats != null) {
                allBeats.Clear();
            } else {
                allBeats = new List<float[]>();
            }
        }

        static void ReadBeats(String beatsfilename) {
            //Reads beats from a file outputted by Sonic Annotator.
            TextReader tr = new StreamReader(beatsfilename);
            
            String line;
            List<float> currentBeatList = new List<float>();
            int filenameCount = 0;

            do {
                line = tr.ReadLine();
                if (line == "" || line == null) break;
                String[] data = line.Split(new char[]{','},StringSplitOptions.None);
                if (data[0].Length > 0) {
                    if (data[0].StartsWith("\"")) { //double-checking format
                        filenames.Add(data[0].Trim(new char[] { '\"' }));
                        if (filenameCount != 0) {
                            allBeats.Add(currentBeatList.ToArray());
                            currentBeatList.Clear();
                        }
                        filenameCount++;
                    }
                }
                //add beat marker
                float beatMarker;
                if (float.TryParse(data[1], out beatMarker)) {
                    currentBeatList.Add(beatMarker);
                }
            } while (true);
            if (filenameCount != 0) allBeats.Add(currentBeatList.ToArray());
            tr.Close();
        }

        static float CompareSongs(AnalysedSong a, AnalysedSong b, out int bestOffset) {
            //Finds the best mashup between song a and song b, measured
            //by how well the frequencies of the two songs correlate,
            //then returns the score of the mashup and the optimal offset 
            //between the two songs.

            int l1 = a.beatHarmonics.GetLength(0);
            int l2 = b.beatHarmonics.GetLength(0);
            float bestSum=float.MaxValue;
            bestOffset = 0;
            
            if (l1 <= l2) {
                for(int offset=0;(offset<l2) && (offset<128);offset++){
                    float sum=0;
                    for(int beat=0;beat<l1;beat++){
                        for(int freq=0;freq<48;freq++){
                            if(beat+offset<l2){
                                float fa=a.beatHarmonics[beat,freq];
                                float fb=b.beatHarmonics[beat+offset,freq];
                                sum += fa * fb;
                                //Alternate scoring method that didn't work quite as well:
                                //sum += Math.Abs(fa - fb) / (fa + fb+10);
                            }
                        }
                    }

                    if (sum < bestSum) {
                        bestOffset = offset;
                        bestSum = sum;
                    }
                }
                return 400*bestSum/Math.Max(l1,l2);
            }else{
                return CompareSongs(b, a, out bestOffset);
            }
        }
    }

    class AnalysedSong{
        public String name;
        public float[] beatTimes;
        public float[,] beatHarmonics;

        public AnalysedSong(String filename, float[] BeatTimes, float[,] BeatHarmonics){
            name=filename;
            beatTimes=BeatTimes;
            beatHarmonics=BeatHarmonics;
        }
    }
}
