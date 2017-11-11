#include <SoftwareSerial.h>// import the serial library
#include <Servo.h>
/**
   Right  hand:
  
   37: Shoulder 1 : Black : 0deg is back // not functioning properly
   35: Shoulder 2 : Gray : 0deg is back
   39: Elbow : White : Range:90-180 : 0deg is back:

   LEft Hand:

   41: Shoulder 1 : Brown : 0deg is back
   53: Shoulder 2 : Green : 0deg is front
   51: Elbow : Blue : 0deg is front

   Left  Leg
   45: Hip : Neon
   47: Knee : Maroon

   Right Leg
   49 : Hip : Orange 
   43: Knee : Red


   Order will be as follows:
   Shoulder 1: Shoulder 2: Elbow: Hip: Knee
   Left then Right
*/


Servo s[6]; //the number here decide the number of servo attached to our arduino
//int s1[] = {6, 25, 5, 24, 7, 26, 9, 22, 10, 23}; //list of correspoding pin numbers s1[0] is the pin nummber of s[0]
//int degintial[] = {90, 90, 180, 0, 90, 120, 100, 0, 180, 145}; 45, 49, 47, 43
int s1[] = {41, 37, 53, 35, 51, 39};
//int degintial[] = {180, 0, 25, 170, 90, 180};
int degintial[] = {180, 180 , 25, 170, 180, 120 }; //V
//int degintial[] = {90, 90, 145, 50, 180, 120};
int deg_min[] ={0  ,0  ,110,90 ,0  ,0  ,0  ,0  ,60  ,0  };
int deg_max[] ={180,180,180,180,180,180,180,110,180,120};
int Range[] = {180, 180, 90, 90, 90, 90 };
int lowerBound[] = {15, 180, 180, 15, 90, 180  };
int upperBound[] = {180, 0, 110, 90, 180, 70 };
//int degintial[] = {33, 28, 23, 13, 167, 174};
//intial position of each motor
Servo S;

String str = "";
int counter = 0;
int deg = 0;
int numberOfDigit = 0;

void setup() {
  Serial.begin(9600);
  for (int i = 0; i <  sizeof(s) / sizeof(*s); i++)
  {
    s[i].attach(s1[i]);
 
    //    s[i].write(0);
    //    delay(150);
    //    s[i].write(180);
    //    delay(150);
    s[i].write(degintial[i]);//sets every motor to its intial position as specified in deginital
    Serial.print("angle: ");
    Serial.print(degintial[i]);
    Serial.print(" -- Pin Number:");
    Serial.println(s1[i]);
    delay(200);
    
  }
  Serial.println("Connected");
  delay(100);

}
void loop()
{

//  for (int i = 0; i <  sizeof(s) / sizeof(*s); i++)
//  {
//   s[i].write(degintial[i]); 
//   delay(15);
//  }
  if (Serial.available())
  {
    char x = char(Serial.read());
    if (x == ':')
    {
      int k  = (atoi(str.c_str()));
      degintial[counter] = k;
      int l = map(k, 0, Range[counter], lowerBound[counter], upperBound[counter]);
      if(k>deg_max[counter])
      {
        s[counter].write(deg_max[counter]); 
      }
      else if(k < deg_min[counter])
      {
         s[counter].write(deg_min[counter]); 
      }
      else
      {
         s[counter].write(l);
      }
      //deg = 0;
      str = "";
      //numberOfDigit =0;
      Serial.print("angle: ");
      Serial.print(k);
      Serial.print(" -- Pin Number:");
      Serial.println(s1[counter]);
      counter = (counter + 1)  % (sizeof(s) / sizeof(*s));
      delay(100);

    }
    else
    {
      str =  str + x;
      //Serial.println(str);
    }

  }

}
