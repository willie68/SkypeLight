#include <RTClib.h>

#include <DHT.h>
#include <DHT_U.h>

#include "Adafruit_NeoPixel.h"
#include <TM1637Display.h>

// DHT Sensor Pin
#define DHTPIN 4
#define DHTTYPE DHT22

// Anzahl der LEDs
#define NUM_LEDS 7

// Datenpin der LEDs
#define DATA_PIN 11
#define CLK 3
#define DIO 2

// Initialisierung der RTC
RTC_DS3231 rtc;
char daysOfTheWeek[7][3] = { "So", "Mo", "Di", "Mi", "Do", "Fr", "Sa" };

// Initialisieren der Jewel Struktur
Adafruit_NeoPixel pixels = Adafruit_NeoPixel(NUM_LEDS, DATA_PIN, NEO_GRB + NEO_KHZ800);
TM1637Display display(CLK, DIO);

// initialisierung DHT
DHT dht(DHTPIN, DHTTYPE);

// values for the display
byte displayValues[4];
byte eselValues[4] = { 249, 109, 249, 56 };
byte easyValues[4] = { 249, 247, 109, 2 + 4 + 32 + 64 };
byte nullValues[4] = { 0, 0, 0, 0 };
byte deg[1] = { SEG_A + SEG_B + SEG_G + SEG_F };
byte hum[1] = { SEG_B + SEG_C + SEG_E + SEG_F + SEG_G };

byte displayBrightness = 15;

void setup() {
  // Initialisieren der seriellen Verbindung zum Host
  Serial.begin(115200);
  // NeoPixel Bibliothek starten
  pixels.begin();
  // Willkommensmeldung ausgeben
  Serial.println("SkypeStatus V0.2");

  display.setBrightness(displayBrightness);
  for (byte i = 0; i < 4; i++) {
    displayValues[i] = 0;
  }
  display.setSegments(displayValues);

  dht.begin();

  if (!rtc.begin()) {
    Serial.println("Couldn't find RTC");
    Serial.flush();
    while (1) delay(10);
  }

  if (rtc.lostPower()) {
    Serial.println("RTC lost power, let's set the time!");
    // When time needs to be set on a new device, or after a power loss, the
    // following line sets the RTC to the date & time this sketch was compiled
    rtc.adjust(DateTime(F(__DATE__), F(__TIME__)));
    // This line sets the RTC with an explicit date & time, for example to set
    // January 21, 2014 at 3am you would call:
    // rtc.adjust(DateTime(2014, 1, 21, 3, 0, 0));
  }
}

long time = 0;
long lc = 0;
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
      Serial.println("no connection");
    }
    delay(1000);
  }
}

void showDHT() {
  float h = dht.readHumidity();
  // Read temperature as Celsius (the default)
  float t = dht.readTemperature();

  // Check if any reads failed and exit early (to try again).
  if (isnan(h) || isnan(t)) {
    Serial.println(F("Failed to read from DHT sensor!"));
    return;
  }

  Serial.print(F("h: "));
  Serial.print(h);
  Serial.print(F("%,t: "));
  Serial.print(t);
  Serial.println(F("°C "));
}

// RGB LEDs
// verarbeiten der Daten vom Host, Format "#index,r,g,b['b']"
// index: 0 für alle LEDs, 1..MAX_LEDS für eine einzelne LED
// r: integer Wert der Farbe Rot 0..255
// g: integer Wert der Farbe Grün 0..255
// b: integer Wert der Farbe Blau 0..255
// b: optionales Zeichen 'b' für blinkend
// Segmentanzeige:
// d15,249,109,249,56* means ESEL
// format für Display d(b),d1,d2,d3,d4*
// wobei b die Helligkeit von 0..7 ist, D1, D2, D3, D4 sind die einzelnen Stellen
// D1,3,4 haben 7 segmente, D2 hat zus. als 8'tes den Doppelpunkt
// Temperatur
// ein einzelnes t zeigt die aktuelle Temperatur an
// Helligkeit der 7-Seg Anzeige
// ein b mit folgender Nummer setzt die Helligkeit der Anzeige, 0..15
// Luftfeuchte
// h zeigt die aktuelle Luftfeuchtigkeit
// Zeit
// z setzt die aktuelle Uhrzeit auf einen neuen Wert
// Info
// ? sendet die aktuellen Informationen

void processPCData() {
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
      displayBrightness = constrain(iBrightness, 0, 15);
      display.setBrightness(displayBrightness);
      Serial.print(displayBrightness);

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
  } else if (myChar == 't') {
    showTemp();
    delay(2000);
  } else if (myChar == 'b') {
    int iBrightness = Serial.parseInt();
    displayBrightness = iBrightness;
    Serial.print("set display brightness to ");
    Serial.println(displayBrightness);
  } else if (myChar == 'h') {
    Serial.println("show humidity");
    showHumidity();
    delay(3000);
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
  } else if (myChar == 'z') {
    // Setting the date time to the clock
    int y = Serial.parseInt();
    int mo = Serial.parseInt();
    int d = Serial.parseInt();
    int h = Serial.parseInt();
    int m = Serial.parseInt();
    int s = Serial.parseInt();
    rtc.adjust(DateTime(y, mo, d, h, m, s));
    Serial.println("time set");
    showRTC();
  } else if (myChar == '?') {
    showDHT();
    showRTC();
  }
}

long timeZ = 0;
bool bShowTime = true;
long actTime = 0;

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
  actTime = millis();
  if (bShowTime) {
    if ((timeZ + 10000) > actTime) {
      display.setColon(((actTime / 1000) % 2) == 0);
      showTime();
    } else {
      bShowTime = false;
      timeZ = actTime;
    }
  } else {
    if ((timeZ + 2000) > actTime) {
      showTemp();
    } else {
      bShowTime = true;
      timeZ = actTime;
    }
  }
  delay(100);
}

long lastT = 0;
int z = 0;

void showTemp() {
  if ((lastT + 1000) < millis()) {
    float t = dht.readTemperature();
    z = t * 10;
    lastT = millis();
  }
  display.setBrightness(displayBrightness);
  display.setColon(true);
  display.showNumberDec(z, false, 3, 0);
  display.setSegments(deg, 1, 3);
}

int h = 0;
long lasth = -1;

void showHumidity() {
  float t = dht.readHumidity();
  h = t;
  display.clear();
  display.setBrightness(displayBrightness);
  display.setColon(false);
  display.showNumberDec(h, false, 2, 0);
  display.setSegments(hum, 1, 3);
  lasth = h;
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
// RTC functions
void showRTC() {
  Serial.print("z: ");
  DateTime now = rtc.now();

  Serial.print(now.year(), DEC);
  Serial.print('/');
  Serial.print(now.month(), DEC);
  Serial.print('/');
  Serial.print(now.day(), DEC);
  Serial.print(" (");
  Serial.print(daysOfTheWeek[now.dayOfTheWeek()]);
  Serial.print(") ");
  Serial.print(now.hour(), DEC);
  Serial.print(':');
  Serial.print(now.minute(), DEC);
  Serial.print(':');
  Serial.print(now.second(), DEC);
  Serial.println();
}

void showTime() {
  DateTime now = rtc.now();
  byte h = now.hour();
  byte m = now.minute();
  display.setBrightness(displayBrightness);
  display.showNumberDec(m, true, 2, 2);
  display.showNumberDec(h, false, 2, 0);
}