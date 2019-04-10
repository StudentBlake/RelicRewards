# Relic Rewards

![main](https://cdn.discordapp.com/attachments/373320120707055617/484055262399823873/sample.PNG)

Automatically get the best value for your relics

## Features
* Picks the best value reward using Warframe.market prices
* Simple game overlay
* Follows Warframe's ToS

## Instructions
* ONLY WORKS AT 1440P RESOLUTION WITH FULL SCALE HUD AT THE MOMENT
* If the number of rewards isn't 4, you must manually adjust using the number pad (Ex: For 3 rewards, you'd have to press 3 on the number pad)
* Press the Print Screen button on the relic rewards screen
* To exit the program, press the Pause/Break button

## Requirements
* Visual Studio 2019
* [**tessdata.zip**](https://github.com/StudentBlake/RelicRewards/releases/download/v0.0/tessdata.zip)

## Build Instructions
* Open **RelicRewards.sln**
* Go to *Build*, then *Build Solution*
* Extract [**tessdata.zip**](https://github.com/StudentBlake/RelicRewards/releases/download/v0.0/tessdata.zip) to `RelicRewards/bin/Debug/` folder
* Run **RelicRewards.exe**

## How does it work?
By pressing Print Screen, the program scans the screen and tries to recognize the Prime part pieces. It then calculates which part is the best one to pick. By default, it priorities Ducat value for any pieces worth under 15 plat.

## Want to help? Feel free to submit a pull request! Here are some ideas:
* Support multiple resolutions (1080p priority, then others)
* Refactor code
* QoL improvements or bug fixes

## Disclaimer
This software is unfinished and unoptimized! Please use with caution.