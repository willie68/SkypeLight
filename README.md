# SkypeLight
A program for showing the actual "Skype for Business"/ Lync status with an external hardware.
The external hardware is an arduino nano v3 with a adafruit neopixel jewel (https://www.adafruit.com/products/2226) and a tm1631 display 
(like this: http://www.seeedstudio.com/wiki/Grove_-_4-Digit_Display)
The jewel is covered with a translucent cube.
The data exchange is very simple, using the USB-COM port driver of the arduino nano. 
See more information on my google+ account.
https://plus.google.com/+WilfriedKlaas/posts/TpiskEiGzVi

The plus version now implements an DHT22 Sensor for temp and humidity and a DS3231 RTC for the time.

For the RGB LED Jewel (7 RGB LEDs) you need the adafruit NeoPixel lib (https://github.com/adafruit/Adafruit_NeoPixel)

For the DHT you need the DHT Sensor Lib (https://github.com/adafruit/DHT-sensor-library)

For the RTC you'll need the adafruit RTCLib (https://github.com/adafruit/RTClib)

All this can be installed via Library manager.

For the TM1637 Display you need my fork of the TM1637Display lib. (https://github.com/willie68/TM1637) This have to be installed manually.

## Controll via Serial Interface

You can control the skyplight via a serial control interface.

The following commands are known:

### RGB LEDs

to control the RGB LEDs you must send a command with the Format "#index,r,g,b['b']" with

- index: 0 for all LEDs, 1..MAX_LEDS for a single LED
- r: integer value of red 0..255
- g: integer value of green 0..255
- b: integer value of blue 0..255
- b: optional char 'b' for blink

### 7-Segment display

sending the command format for the display "d(b),d1,d2,d3,d4*"

example d15,249,109,249,56* means ESEL

- b brightness from 0..15
- D1, D2, D3, D4 are the single segments
-  D1,3,4 have 7 segments, D2 has the colon as an 8's 

### Brightness of the 7-Seg Display

b sets the brightness of the default display, 0..15

### Temperature

a single T show the actual temperature (from the DHT 22 Sensor)

### Humidity

h show the actual humidity 

### Time

z sets the actual time of the RTC, format z yyyy,mm,dd,hh,mm,ss

### Date

r show the actual date

### Info

With an ? you can read internal sensor data from the RTC and DHT
