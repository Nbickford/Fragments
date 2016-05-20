
Sonic Annotator
===============

Sonic Annotator is a utility program for batch feature extraction from
audio files.  It runs Vamp audio analysis plugins on audio files, and
can write the result features in a selection of formats.

For more information, see

  http://www.omras2.org/SonicAnnotator

More documentation follows further down this README file, after the
credits.


Credits
-------

Sonic Annotator was developed at the Centre for Digital Music,
Queen Mary, University of London.

  http://www.elec.qmul.ac.uk/digitalmusic/

The main program is by Mark Levy, Chris Cannam, and Chris Sutton.
Sonic Annotator incorporates library code from the Sonic Visualiser
application by Chris Cannam.  Code copyright 2005-2007 Chris Cannam,
copyright 2006-2011 Queen Mary, University of London, except where
indicated in the individual source files.

This work was funded by the Engineering and Physical Sciences Research
Council through the OMRAS2 project EP/E017614/1.

Sonic Annotator is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License as
published by the Free Software Foundation; either version 2 of the
License, or (at your option) any later version.  See the file COPYING
included with this distribution for more information.

Sonic Annotator may also make use of the following libraries:

 * Qt4 -- Copyright Nokia Corporation, distributed under the GPL
 * Ogg decoder -- Copyright CSIRO Australia, BSD license
 * MAD mp3 decoder -- Copyright Underbit Technologies Inc, GPL
 * libsamplerate -- Copyright Erik de Castro Lopo, GPL
 * libsndfile -- Copyright Erik de Castro Lopo, LGPL
 * FFTW3 -- Copyright Matteo Frigo and MIT, GPL
 * Vamp plugin SDK -- Copyright Chris Cannam, BSD license
 * Redland RDF libraries -- Copyright Dave Beckett and the University of Bristol, LGPL/Apache license

(Some distributions of Sonic Annotator may have one or more of these
libraries statically linked.)  Many thanks to their authors.

Sonic Annotator can also use QuickTime for audio file import on OS/X.
For licensing reasons, you may not distribute binaries of Sonic
Annotator with QuickTime support included for any platform that does
not include QuickTime as part of the platform itself (see section 3 of
version 2 of the GNU General Public License).


Compiling Sonic Annotator
--------------------------

If you are planning to compile Sonic Annotator from source code,
please read the file INSTALL.


A Quick Tutorial
================

To use Sonic Annotator, you need to tell it three things: what audio
files to extract features from; what features to extract; and how and
where to write the results.  You can also optionally tell it to
summarise the features.


1. What audio files to extract features from

Sonic Annotator accepts a list of audio files on the command line.
Any argument that is not understood as a supported command-line option
will be taken to be the name of an audio file.  Any number of files
may be listed.

Several common audio file formats are supported, including MP3, Ogg,
and a number of PCM formats such as WAV and AIFF.  AAC is supported on
OS/X only, and only if not DRM protected.  WMA is not supported.

File paths do not have to be local; you can also provide remote HTTP
or FTP URLs for Sonic Annotator to retrieve.

Sonic Annotator also accepts the names of playlist files (.m3u
extension) and will process every file found in the playlist.

Finally, you can provide a local directory path instead of a file,
together with the -r (recursive) option, for Sonic Annotator to
process every audio file found in that directory or any of its
subdirectories.


2. What features to extract

Sonic Annotator applies "transforms" to its input audio files, where a
transform (in this terminology) consists of a Vamp plugin together
with a certain set of parameters and a specified execution context:
step and block size, sample rate, etc.

(See http://www.vamp-plugins.org/ for more information about Vamp
plugins.)

To use a particular transform, specify its filename on the command
line with the -t option.

Transforms are usually described in RDF, following the transform part
of the Vamp plugin ontology (http://purl.org/ontology/vamp/).  A
Transform may use any Vamp plugin that is currently installed and
available on the system.  You can obtain a list of available plugin
outputs by running Sonic Annotator with the -l option, and you can
obtain a skeleton transform description for one of these plugins with
the -s option.

For example, if the example plugins from the Vamp plugin SDK are
available and no other plugins are installed, you might have an
exchange like this:

  $ sonic-annotator -l
  vamp:vamp-example-plugins:amplitudefollower:amplitude
  vamp:vamp-example-plugins:fixedtempo:acf
  vamp:vamp-example-plugins:fixedtempo:detectionfunction
  vamp:vamp-example-plugins:fixedtempo:filtered_acf
  vamp:vamp-example-plugins:fixedtempo:tempo
  vamp:vamp-example-plugins:fixedtempo:candidates
  vamp:vamp-example-plugins:percussiononsets:detectionfunction
  vamp:vamp-example-plugins:percussiononsets:onsets
  vamp:vamp-example-plugins:powerspectrum:powerspectrum
  vamp:vamp-example-plugins:spectralcentroid:linearcentroid
  vamp:vamp-example-plugins:spectralcentroid:logcentroid
  vamp:vamp-example-plugins:zerocrossing:counts
  vamp:vamp-example-plugins:zerocrossing:zerocrossings
  $ sonic-annotator -s vamp:vamp-example-plugins:fixedtempo:tempo
  @prefix xsd:      <http://www.w3.org/2001/XMLSchema#> .
  @prefix vamp:     <http://purl.org/ontology/vamp/> .
  @prefix :         <#> .

  :transform a vamp:Transform ;
      vamp:plugin <http://vamp-plugins.org/rdf/plugins/vamp-example-plugins#fixedtempo> ;
      vamp:step_size "64"^^xsd:int ; 
      vamp:block_size "256"^^xsd:int ; 
      vamp:parameter_binding [
          vamp:parameter [ vamp:identifier "maxbpm" ] ;
          vamp:value "190"^^xsd:float ;
      ] ;
      vamp:parameter_binding [
          vamp:parameter [ vamp:identifier "maxdflen" ] ;
          vamp:value "10"^^xsd:float ;
      ] ;
      vamp:parameter_binding [
          vamp:parameter [ vamp:identifier "minbpm" ] ;
          vamp:value "50"^^xsd:float ;
      ] ;
      vamp:output <http://vamp-plugins.org/rdf/plugins/vamp-example-plugins#fixedtempo_output_tempo> .
  $

The output of -s is an RDF/Turtle document describing the default
settings for the Tempo output of the Fixed Tempo Estimator plugin in
the Vamp plugin SDK.

(The exact format of the RDF printed may differ -- e.g. if the
plugin's RDF description is not installed and so its "home" URI is not
known -- but the result should be functionally equivalent to this.)

You could run this transform by saving the RDF to a file and
specifying that file with -t:

  $ sonic-annotator -s vamp:vamp-example-plugins:fixedtempo:tempo > test.n3
  $ sonic-annotator -t test.n3 audio.wav -w csv --csv-stdout
  (... logging output on stderr, then ...)
  "audio.wav",0.002902494,5.196916099,68.7916,"68.8 bpm"
  $

The single line of output above consists of the audio file name, the
timestamp and duration for a single feature, the value of that feature
(the estimated tempo of the given region of time from that file, in
bpm -- the plugin in question performs a single tempo estimation and
nothing else) and the feature's label.

A quicker way to achieve the above is to use the -d (default) option
to tell Sonic Annotator to use directly the default configuration for
a named transform:

  $ sonic-annotator -d vamp:vamp-example-plugins:fixedtempo:tempo audio.wav -w csv --csv-stdout
  (... some log output on stderr, then ...)
  "audio.wav",0.002902494,5.196916099,68.7916,"68.8 bpm"
  $

Although handy for experimentation, the -d option is inadvisable in
any "production" situation because the plugin configuration is not
guaranteed to be the same each time (for example if an updated version
of a plugin changes some of its defaults).  It's better to save a
well-defined transform to file and refer to that, even if it is simply
the transform created by the skeleton option.

To run more than one transform on the same audio files, just put more
than one set of transform RDF descriptions in the same file, or give
the -t option more than once with separate transform description
files.  Remember that if you want to specify more than one transform
in the same file, they will need to have distinct URIs (that is, the
":transform" part of the example above, which may be any arbitrary
name, must be distinct for each described transform).


3. How and where to write the results

Sonic Annotator supports various different output modules (and it is
fairly easy for the developer to add new ones).  You have to choose at
least one output module; use the -w (writer) option to do so.  Each
module has its own set of parameters which can be adjusted on the
command line, as well as its own default rules about where to write
the results.

The following writers are currently supported.  (Others exist, but are
not properly implemented or not supported.)

 * csv

   Writes the results into comma-separated data files.

   One file is created for each transform applied to each input audio
   file, named after the input audio file and transform name with .csv
   suffix and ":" replaced by "_" throughout, placed in the same
   directory as the audio file.

   To instruct Sonic Annotator to place the output files in another
   location, use --csv-basedir with a directory name.

   To write a single file with all data in it, use --csv-one-file.

   To write all data to stdout instead of to a file, use --csv-stdout.

   Sonic Annotator will not write to an output file that already
   exists.  If you want to make it do this, use --csv-force to
   overwrite or --csv-append to append to it.

   The data generated consists of one line for each result feature,
   containing the feature timestamp, feature duration if present, all
   of the feature's bin values in order, followed by the feature's
   label if present.  If the --csv-one-file or --csv-stdout option is
   specified, then an additional column will appear before any of the
   above, containing the audio file name from which the feature was
   extracted, if it differs from that of the previous row.

   The default column separator is a comma; you can specify a
   different one with the --csv-separator option.

 * rdf

   Writes the results into RDF/Turtle documents following the Audio
   Features ontology (http://purl.org/ontology/af/).

   One file is created for each input audio file containing the
   features extracted by all transforms applied to that file, named
   after the input audio file with .n3 extension, placed in the same
   directory as the audio file.

   To instruct Sonic Annotator to place the output files in another
   location, use --rdf-basedir with a directory name.

   To write a single file with all data (from all input audio files)
   in it, use --rdf-one-file.

   To write one file for each transform applied to each input audio
   file, named after the input audio file and transform name with .n3
   suffix and ":" replaced by "_" throughout, use --rdf-many-files.

   To write all data to stdout instead of to a file, use --rdf-stdout.

   Sonic Annotator will not write to an output file that already
   exists.  If you want to make it do this, use --rdf-force to
   overwrite or --rdf-append to append to it.

   Sonic Annotator will use plugin description RDF if available to
   enhance its output (for example identifying note onset times as
   note onset times, if the plugin's RDF says that is what it
   produces, rather than writing them as plain events).  Best results
   will be obtained if an RDF document is provided with your plugins
   (for example, vamp-example-plugins.n3) and you have this installed
   in the same location as the plugins.  To override this enhanced
   output and write plain events for all features, use --rdf-plain.

   The output RDF will include an available_as property linking the
   results to the original audio signal URI.  By default, this will
   point to the URI of the file or resource containing the audio that
   Sonic Annotator processed, such as the file:/// location on disk.
   To override this, for example to process a local copy of a file
   while generating RDF that describes a copy of it available on a
   network, you can use the --rdf-signal-uri option to specify an
   alternative signal URI.


4. Optionally, how to summarise the features

Sonic Annotator can also calculate and write summaries of features,
such as mean and median values.

To obtain a summary as well as the feature results, just use the -S
option, naming the type of summary you want (min, max, mean, median,
mode, sum, variance, sd or count).  You can also tell it to produce
only the summary, not the individual features, with --summary-only.

Alternatively, you can specify a summary in a transform description.
The following example tells Sonic Annotator to write both the times of
note onsets estimated by the simple percussion onset detector example
plugin, and the variance of the plugin's onset detection function.
(It will only process the audio file and run the plugin once.)

  @prefix rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>.
  @prefix vamp: <http://purl.org/ontology/vamp/>.
  @prefix examples: <http://vamp-plugins.org/rdf/plugins/vamp-example-plugins#>.
  @prefix : <#>.

  :transform1 a vamp:Transform;
     vamp:plugin examples:percussiononsets ;
     vamp:output examples:percussiononsets_output_onsets .

  :transform0 a vamp:Transform;
     vamp:plugin examples:percussiononsets ;
     vamp:output examples:percussiononsets_output_detectionfunction ;
     vamp:summary_type "variance" .

Sonic Annotator can also summarise in segments -- if you provide a
comma-separated list of times as an argument to the --segments option,
it will calculate one summary for each segment bounded by the times
you provided.  For example,

  $ sonic-annotator -d vamp:vamp-example-plugins:percussiononsets:detectionfunction -S variance --sumary-only --segments 1,2,3 -w csv --csv-stdout audio.wav
  (... some log output on stderr, then ...)
  ,0.000000000,1.000000000,variance,1723.99,"(variance, continuous-time average)"
  ,1.000000000,1.000000000,variance,1981.75,"(variance, continuous-time average)"
  ,2.000000000,1.000000000,variance,1248.79,"(variance, continuous-time average)"
  ,3.000000000,7.031020407,variance,1030.06,"(variance, continuous-time average)"

Here the first row contains a summary covering the time period from 0
to 1 second, the second from 1 to 2 seconds, the third from 2 to 3
seconds and the fourth from 3 seconds to the end of the (short) audio
file.

