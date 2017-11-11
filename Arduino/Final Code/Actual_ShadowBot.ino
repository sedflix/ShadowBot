#include <SoftwareSerial.h>// import the serial library
#include <Servo.h>
/**
   Right  hand:

   37: Shoulder 1 : Black : 0deg - straight or down, 180 means - Up
   35: Shoulder 2 : Gray : 15deg - straight, 90 deg: Maximum stretch
   39: Elbow : White : Range:90-180 : 0deg is back:

   LEft Hand:

   41: Shoulder 1 : Brown : 180: down, 15: UP //Fix 180 to be straight
   53: Shoulder 2 : Green : 0deg is front
   51: Elbow : Blue : 0deg is front

   Left  Leg
   45: Hip : Neon
   47: Knee : Maroon

   Right Leg
   33 : Hip : Orange
   43: Knee : Red


   Order will be as follows:
   Shoulder 1: Shoulder 2: Elbow: Hip: Knee
   Left then Right
*/

Servo s[10]; //the number here decide the number of servo attached to our arduino
int s1[] = {41, 37, 53, 24, 51, 39, 45, 33, 47, 43};
int deginitial[] = {180, 0 , 180, 50, 90, 180 , 40, 0, 60, 20 }; //V
int deg_min[] = {0  , 0  , 110, 50 , 80  , 70  , 70  , 40  , 60  , 20 };
int deg_max[] = {180, 180, 180, 120 , 165, 170, 180, 130, 180, 110};
int lowerBound[] = {0, 180, 180, 50, 80, 170, 70, 130, 180, 20};
int upperBound[] = {180, 0, 110, 120, 165, 70 , 180, 40, 90, 110};
int Range[] = {180, 180, 90, 90, 90, 90, 90, 90, 90, 90};

String str = "";
int deg = 0;
int motor_number = 0;
boolean wrong_motor_flag = false;

void setup() {
  Serial.begin(9600);
  Serial3.begin(9600);
  for (int i = 0; i <  sizeof(s) / sizeof(*s); i++)
  {
    s[i].attach(s1[i]);
    s[i].write(deginitial[i]);//sets every motor to its intial position as specified in deginital
    Serial.print("angle: ");
    Serial.print(deginitial[i]);
    Serial.print(" -- Pin Number:");
    Serial.println(s1[i]);
    delay(200);
  }
  Serial.println("Connected");
  delay(100);

}
void loop()
{

  if (Serial3.available())
  {
    char x = char(Serial3.read());
    //Serial.println(x);
    if (x == ':')
    {
      int d1 = map(deginitial[0], deg_min[0], deg_max[0], 0, Range[0]); //Left 1
      int d2 = map(deginitial[1], deg_min[1], deg_max[1], 0, Range[1]); //Right 1
      int d3 = map(deginitial[2], deg_min[2], deg_max[2], 0, Range[2]); //Left 2
      int d4 = map(deginitial[3], deg_min[3], deg_max[3], 0, Range[3]); //Right 2
      if (d1 >= 140 && d1 <= 180)
      {
        deg_max[4] = map(2 * (180 - d3), 0, 90, deg_min[4], 180);
      }
      else
      {
        deg_max[4] = 180;
      }
      if (d2 >= 140 && d2 <= 180)
      {
        deg_max[5] = map(2 * (180 - d4), 0, 90, deg_min[5], 180);
      }
      else
      {
        deg_max[5] = 180;
      }
      if ( ! wrong_motor_flag)
      {
        int angle  = (atoi(str.c_str()));
        deginitial[motor_number] = angle;
        int maped_angle = map(angle, 0 , Range[motor_number] , lowerBound[motor_number], upperBound[motor_number]);
        if (! (maped_angle > deg_max[motor_number] || maped_angle < deg_min[motor_number] ))
        {
          s[motor_number].write(maped_angle);
          //Serial.print("angle: ");
          //Serial.print(s[motor_number].read());
          //Serial.print(" -- Pin Number:");
          //Serial.println(s1[motor_number]);
        }
        else
        {
          Serial.println("Glitch");
        }

      }
      else
      {
        Serial.println("Wrong Motor");
      }
      wrong_motor_flag = false;
      str = "";
      //delay(15);
    }
    else if (x == '.')
    {
      motor_number  = (atoi(str.c_str()));
      if (motor_number > sizeof(s) / sizeof(*s) || motor_number < 0 )
      {
        wrong_motor_flag = true;
      }
      str = "";
    }
    else
    {
      str =  str + x;
    }

  }

}
