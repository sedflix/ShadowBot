#include <SoftwareSerial.h>
SoftwareSerial BTSerial(19, 20);
void setup() {
  Serial.begin(9600);
  BTSerial.begin(9600);
}
String str = "";
int w = 0;
void loop() {
  if (BTSerial.available())
  {
    char x = char(BTSerial.read());

    if (x == ':')
    {
      int k  = (atoi(str.c_str()));
      w = k;
      str = "";
      Serial.println(k);
    }
    else
    {
      str =  str + x;
    }
  }
}

