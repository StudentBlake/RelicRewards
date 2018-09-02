# Relic Rewards

![main](https://cdn.discordapp.com/attachments/373320120707055617/484055262399823873/sample.PNG)

Automatically get the best value for your relics

## Features
* Picks the best value reward using Warframe.market prices
* Simple game overlay
* Follows Warframe's ToS

## Instructions
* ONLY WORKS AT 1440P RESOLUTION WITH FULL SCALE HUD AT THE MOMENT
* Press the Print Screen button on the relic rewards screen
* If the number of rewards isn't 4, you must manually adjust using the number pad (for example, 3 rewards would be Num3)
* To exit the program, press the Pause button

## Requirements
* Visual Studio 2017
* NuGet Package Manager

## Build Instructions
* Open **RelicRewards.sln**
* Go to *Build*, then *Build Solution*
* Extract [**tessdata.zip**](https://github.com/StudentBlake/RelicRewards/releases/download/v0.0/tessdata.zip) to `RelicRewards/bin/Debug/` folder
* Run **RelicRewards.exe**

## How does it work?
As soon as you press Print Screen, the program takes a picture of the relic rewards. Once that happens, it tries to improve the picture to make it more readable for the program. 
Using Tesseract (and OCR library), it recognizes the text and compares the result against a list of items from Warframe.market. 
This will always find a positive match no matter what against the current item list from Warframe.market. 
Once this is completed, it will fetch the 5 top currently online in game orders and find the average. 
The Ducat JSON is cached because the value never changes, but online orders are always refreshed. 
The program will calculate the best pick based on Platinum and Ducats and display the result.

## Want to help? Submit a pull request for any of the following:
* Support multiple resolutions (1080p priority, then others)
* Potential bug where the program randomly stops recognizing inputs
* Refactor code
* QoL improvements or bug fixes

## Disclaimer
This software is unfinished and unoptimized! Please use with caution.