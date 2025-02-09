# TeslaLogger-TezLab-Import
[![Contributors][contributors-shield]][contributors-url] [![Forks][forks-shield]][forks-url] [![Stargazers][stars-shield]][stars-url] [![Issues][issues-shield]][issues-url]

Feel free to use this quick and dirty Importer from TezLab export files to [TeslaLogger](https://github.com/bassmaster187/TeslaLogger). 

Get the source of this repository and open it in Visual Studio (Visual Studio Code could work too, but I never tried it).

1. Open Program.cs and change the commented lines. The most important line will be the DBConnectionString. Check it!
1. Move the CSV files from TezLab to the following directories ChargingCsvFiles and DrivingCsvFiles.
1. Run the application!
1. After the import: refresh the teslalogger - it will geocode your locations (but be patient, it waits 10 seconds after every location)!

Sorry, but there will be no support on this code at all. If it works for you, well great! If not, bad luck.

Contact me, if you want to maintain this code.

<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[contributors-shield]: https://img.shields.io/github/contributors/dschuesae/TeslaLogger-TezLab-Import.svg?style=for-the-badge
[contributors-url]: https://github.com/dschuesae/TeslaLogger-TezLab-Import/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/dschuesae/TeslaLogger-TezLab-Import.svg?style=for-the-badge
[forks-url]: https://github.com/dschuesae/TeslaLogger-TezLab-Import/network/members
[stars-shield]: https://img.shields.io/github/stars/dschuesae/TeslaLogger-TezLab-Import.svg?style=for-the-badge
[stars-url]: https://github.com/dschuesae/TeslaLogger-TezLab-Import/stargazers
[issues-shield]: https://img.shields.io/github/issues/dschuesae/TeslaLogger-TezLab-Import.svg?style=for-the-badge
[issues-url]: https://github.com/dschuesae/TeslaLogger-TezLab-Import/issue
