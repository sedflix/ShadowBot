#include <SoftwareSerial.h>// import the serial library



SoftwareSerial Genotronex(6, 7); // RX, TX

void setup() {

  Genotronex.begin(9600);
  Serial.begin(9600);

}

void loop() {

  if (Genotronex.available()) {
   
    Serial.println(Genotronex.read());
  }
}

