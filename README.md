OvenController
==============

A control software for the (now retired) [SparkFun Reflow Toaster Controller here](https://www.sparkfun.com/products/retired/81)

This requires the firmware on the Toaster controller to be replaced.

Background
----------

The LCD screen broke on my Toaster Controller while I was producing my Ethernet Relay boards. I searched around and eventually ordered a new screen from Spark Fun, however shipping could take a couple of weeks, I needed a quick solution.

This project was hacked together in a night, and has been slowly improved over time. I had grand plans to implement saving/loading profiles from a SQLite database, but that has been abandoned for now. The reflow curve is hard coded.

The basic premise of the control loop is to keep the rate of temperature change between a minimum and maximum value.

The interface shows a graph of the current run on screen, as visual confirmation of the reflow curve.