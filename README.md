# WalkmanManager
A easy-to-use app to replace Sony MediaCenter with more features and better design

## The author says `!Important`

**This project is still under construction** and the first release will probably come by September ~~(if i work hard)~~.
I will put a lot of hard work on performance and resource usage. I have been putting a lot of time testing and finding the best solution and the algorithm. Therefore, the release date may delay. I will do whatever I can to create the best user experience because I really understand how it feels using buggy and slow apps.
**If you want to contribute to this project, continue reading.**

## Getting Started

This project is designed under WPF which gives a better user experience. The following sections will get you started to contribute your code to this project if you are interested.

## Building

To build this project, you need the following software

- Visual Studio 2017 (Visual Studio 2015 should work also but not tested)

You will also need to install the following packages in order to build this project successfully
 - MaterialDesignThemes
 - SQLite
 - atl

 To Install Packages, open up the nuget package manager console and copy the code

    Install-Package MaterialDesignThemes -Version 2.5.0-ci1122
	Install-Package System.Data.SQLite -Version 1.0.108
	Install-Package z440.atl.core -Version 2.4.2

## Third-Party Library

* [MaterialDesignThemes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
* SQLite
* [atldotnet](https://github.com/Zeugma440/atldotnet)
I used this library because ~~I am lazy~~ it is convenient

## Contributing

Fork our code to your repository and make changes directly. Send us a pull request when you are done. If you have any questions on anything, feel free to ask. Create an issue and I will reply you as soon as possible (probably within 10 minutes if I have network).

### Utility Classes
I made a few classes to manage the database and I wrote usage on almost all of them. Please use those methods if there is one suits your situation. You can add more functions to those classes if you feel the method will be reused for times. On those methods, I commonly wrote 3 overloads, one using short connection to the database, one uses custom `SQLiteConnection` Object and one uses custom `SQLiteCommand` Object. These methods should help you write more efficient code. If you are processing a bunch of data, please use a transaction to improve efficiency.
**Thank you for reading this far. We are really looking forward to your pull request. If you need help, I am always here. Thanks again.**

## Authors

* [**Kelly**](https://github.com/guo40020) - *Initial work*
* [**Banyc**](https://github.com/Banyc) - *Initial work* for files in ".\WalkmanManager\music_files_management\" and for this "README.md"

See also [contributors](https://github.com/guo40020/WalkmanManager/graphs/contributors)

## License

This project is licensed under the GPL-3.0 License - see the [LICENSE](LICENSE) file for details

