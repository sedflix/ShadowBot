void setup() {
    Serial1.begin(9600);
    Serial.begin(9600);

}

void loop() {
  if (Serial1.available()) {
   
    Serial.println(Serial1.read());
  }
}
