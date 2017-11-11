#include <SoftwareSerial.h>// import the serial library
#include <Servo.h>

Servo s;

void setup() {
  Serial.begin(9600);
  s.attach(43);
}
String str;
int w=0;
void loop() {
  if (Serial.available())
  {
    char x = char(Serial.read());
    if (x == ':')
    {
      int k  = (atoi(str.c_str()));
      s.write(k);
      w=k;
      str = "";
      Serial.println(k);
    }
    else
    {
      str =  str + x;
    }
  }
  s.write(w);
}
