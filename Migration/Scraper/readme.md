# Scraper for C1 CMS

Tool that may be helpfull when having to move content from a website to C1 CMS. Be aware this is a bit raw...

Provided you can pair multi lingual pages under a common id (by examining the URL, page html or via your own lookup) the tool can import a multi lingual website in a nice way.

Also imports media files and ensure internal linking is good.

The tool will download everything on the first run into a local cache folder and then run 100% offline, using what was downloaded into this folder. This make re-running the import a lot faster, which
you probably will, as you customize the content parser.

## Output

This will output raw media and data xml files (which would overwrite existing data on import). In a future version, generating a package that add data/media would make sense.

## Using this

Step 1 is to set up a local test site:

1) Set up a Venus starter site locally
2) To fully appreciate the sample parser, register the content languages en-US (US English) and zh-CN (Chinese) 
3) Update the path to your website in this projects program.cs file
4) Run - provided the website used for the sample parser has not changed, you should see a website being imported and show up on your test site.

After seeing the basic setup running:

1) In program.cs, modify the homepage URLs and cultures for the website you need to import
2) Modify /CustomProviders/Samples/ContentParser.cs (or implement IContentParser in a new class and wire it up in program.cs).

Most of your work will go into customizing a class implementing IContentParser - this class has to figure out what sections on a page contains structured navigation, deliver page id's,
what html fragments should go into the CMS data store etc.

You may also want to modify the class implementing ITemplateChooser if you are not using default starter site templates.