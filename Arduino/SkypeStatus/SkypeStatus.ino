#include "Adafruit_NeoPixel.h"
#include <TM1637Display.h>

// Anzahl der LEDs
#define NUM_LEDS 7

// Datenpin der LEDs
#define DATA_PIN 11
#define CLK 3
#define DIO 2

// Initialisieren der Jewel Struktur
Adafruit_NeoPixel pixels = Adafruit_NeoPixel(NUM_LEDS, DATA_PIN, NEO_GRB + NEO_KHZ800);
TM1637Display display(CLK, DIO);

// values for the display
byte displayValues[4];
byte eselValues[4] = {249, 109, 249, 56};
byte easyValues[4] = {249, 247, 109, 2+4+32+64};

void setup() {
  // Initialisieren der seriellen Verbindung zum Host
  Serial.begin(9600);
  // NeoPixel Bibliothek starten
  pixels.begin();
  // Willkommensmeldung ausgeben
  Serial.println("SkypeStatus V0.1");

  display.setBrightness(0x0f);
  for (byte i = 0; i < 4; i++) {
    displayValues[i] = 0;
  }
  display.setSegments(displayValues);
}

long time = 0;
boolean blink = false;
boolean showColon = false;
boolean connect = false;
// 1. Farbe nach dem Start soll rot sein
byte red = 64, green = 0, blue = 0;

void loop() {
  if (Serial.available() > 0) {
    processPCData();
  } else if (!connect) {
    fadeAllColors();
  } else {
    if (blink) {
      if ((time % 2) == 0) {
        pixels.setBrightness(255);
      } else {
        pixels.setBrightness(1);
      }
    }
    pixels.show();

    if (showColon) {
      if ((time % 2) == 0) {
        display.setColon(true);
      } else {
        display.setColon(false);
      }
    } else {
      display.setColon(true);
    }
    display.setSegments(displayValues);

    time++;
    // wenn Verbindung zum Host besteht, muss jede Minute ein Update kommen
    if (time > 60) {
      time = 0;
      connect = false;
    }
    delay(1000);
  }
}

// verarbeiten der Daten vom Host, Format "#index,r,g,b['b']"
// index: 0 für alle LEDs, 1..MAX_LEDS für eine einzelne LED
// r: integer Wert der Farbe Rot 0..255
// g: integer Wert der Farbe Grün 0..255
// b: integer Wert der Farbe Blau 0..255
// b: optionales Zeichen 'b' für blinkend
//d15,249,109,249,56* means ESEL
// format für Display d(b),d1,d2,d3,d4*
// wobei b die Helligkeit von 0..7 ist, D1, D2, D3, D4 sind die einzelnen Stellen
// D1,3,4 haben 7 segmente, D2 hat zus. als 8'tes den Doppelpunkt

void  processPCData() {
  // erstes Zeichen muss ein '#' sein
  char myChar = Serial.read();
  if (myChar == 'd') {
    int iBrightness = Serial.parseInt();
    int c1 = Serial.parseInt();
    int c2 = Serial.parseInt();
    int c3 = Serial.parseInt();
    int c4 = Serial.parseInt();

    myChar = Serial.read();
    if (myChar == ',') {
      myChar = Serial.read();
    }
    if (myChar == 'b') {
      showColon = true;
      myChar = Serial.read();
    } else {
      showColon = false;
    }
    if (myChar == '*') {
      Serial.print("time: b");
      byte brightness = constrain(iBrightness, 0, 15);
      display.setBrightness(brightness);
      Serial.print(brightness);

      Serial.print(", seg:");
      byte value = constrain(c1, 0, 255);
      displayValues[0] = value;
      Serial.print(value, HEX);
      Serial.print(",");
      value = constrain(c2, 0, 255);
      displayValues[1] = value;
      Serial.print(value, HEX);
      Serial.print(",");
      value = constrain(c3, 0, 255);
      displayValues[2] = value;
      Serial.print(value, HEX);
      Serial.print(",");
      value = constrain(c4, 0, 255);
      displayValues[3] = value;
      Serial.print(value, HEX);
      display.setSegments(displayValues);
      Serial.print(", c:");
      Serial.print(showColon);
      Serial.println(" changed");
      time = 0;
      connect = true;
    }
  } else if (myChar == '#') {
    // Jetzt zunächst den Index und die 3 Farben lesen
    // Danach das b oder das Ende-Kennzeichen
    int index = Serial.parseInt();
    int red = Serial.parseInt();
    int green = Serial.parseInt();
    int blue = Serial.parseInt();
    byte myChar = Serial.read();
    if (myChar == ',') {
      myChar = Serial.read();
    }
    if (myChar == 'b') {
      blink = true;
      myChar = Serial.read();
    } else {
      blink = false;
    }
    if (myChar == '*') {
      index = constrain(index, 0, NUM_LEDS);
      red = constrain(red, 0, 255);
      green = constrain(green, 0, 255);
      blue = constrain(blue, 0, 255);

      Serial.print(index);
      Serial.print(':');
      Serial.print(red, HEX);
      Serial.print('.');
      Serial.print(green, HEX);
      Serial.print('.');
      Serial.println(blue, HEX);
      clear();
      pixels.setBrightness(255);
      showColor(index, pixels.Color(red, green, blue));
      time = 0;
      connect = true;
    } else {
      clear();
    }
  }
}

// Alle Farben durchgehen, aber bitte nicht zu hell...
void fadeAllColors() {
  if (red == 64) {
    red = 63;
    green = 1;
    blue = 0;
  } else if (red > 0 && green < 64 && blue == 0) {
    red--;
    green++;
  } else if (green == 64) {
    green = 63;
    blue = 1;
    red = 0;
  } else if (green > 0 && blue < 64 && red == 0) {
    green--;
    blue++;
  } else if (blue == 64) {
    blue = 63;
    red = 1;
    green = 0;
  } else if (blue > 0 && red < 64 && green == 0) {
    blue--;
    red++;
  }
  blink = false;
  showColor(0, pixels.Color(red, green, blue));

  display.setBrightness(0x04);
  display.setColon(false);
  display.setSegments(easyValues);
  delay(100);
}

// eine LED auf Farbe setzen
void setLED(byte index, uint32_t color) {
  index = index % NUM_LEDS;
  pixels.setPixelColor(index, color);
}

// Farbe für die gewünschten LED ssetzen
void showColor(int index, uint32_t color) {
  if (index == 0) {
    // Farbe für alle LEDs setzen
    for (int i = 0; i < NUM_LEDS; i++) {
      setLED(i, color);
    }
  } else {
    // Farbe nur für eine LED setzen
    setLED(index - 1, color);
  }
  // jetzt anzeigen
  pixels.show();
}

// alle Farben löschen
void clear() {
  for (int i = 0; i < NUM_LEDS; i++) {
    setLED(i, pixels.Color(0, 0, 0));
  }
  pixels.show();
}
