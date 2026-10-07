namespace ServiceGameV2 {
 // V19 script: every spoken line, note and inner-voice subtitle. Generated from Sources/V19/script.json; edit there.
 public static class ServiceScript {
  public sealed class PropertyText{public string Docket,Note,NoteNight2,Instructions,Inside,Reveal,Death;public string[] Arrival;}
  static readonly PropertyText[] properties={
   new PropertyText{Docket="Correll residence",Note="",NoteNight2="",Instructions="Knock. Serve Walter Correll in person at the front door.",Inside="",Reveal="",Death="",Arrival=new[]{"Porch light's on. Dog in the yard. Somebody actually lives out here.","Correll again. Rex remembers me. Doesn't like me any better.",""}},
   new PropertyText{Docket="Vale House",Note="Can't come down. Let yourself in.\nRight-hand stairs, left at the landing.\nStudy's the first door on the left.\nDesk is fine.\nNo need to come find me.\n- M. Vale",NoteNight2="Lamp's on in the study. Desk is fine.\nDon't mind the noise upstairs.\nIt's the house.\n- M. Vale",Instructions="Right-hand stairs, left at the landing. The study is the first door on the left. Papers on the desk.",Inside="Hello? County. Just dropping something off. I'll be quick.",Reveal="Something just came out into the hall. It saw me.",Death="Vale House was searched the next morning. The study door was locked.",Arrival=new[]{"Vale House. Too much house for this road. Too many windows.","Vale again. Right stairs, left, first door, desk, out. Don't think about it.","Vale. Third time. The only name on tonight's sheet I recognize."}},
   new PropertyText{Docket="Unsurveyed parcel",Note="Come in out of the rain.\nPost on the table past the sitting room.\nYou know the way.\nWe kept the light on.\nDon't bother knocking next time.",NoteNight2="",Instructions="Through the sitting room. Leave the post on the table beyond it.",Inside="It's warm in here. First warm house I've been in all week.",Reveal="Car's running out there. The keys are in my pocket.",Death="",Arrival=new[]{"","","Same cabin as Correll's. Same porch. No dog. Same builder, probably."}},
   new PropertyText{Docket="Harrow Lodge",Note="County - it's not locked. Come in.\nLeave it on the table by the lamp,\nfar end of the hall.\nI leave the lamp on. Always have.\n- E. Harrow",NoteNight2="",Instructions="Down the hall to the lamp at the far end. Papers on the table beside it.",Inside="Smells like nobody's opened a window in here since the fifties.",Reveal="That's breathing. Right behind me. It isn't mine.",Death="Papers were found on the table at Harrow Lodge. The server was not.",Arrival=new[]{"Harrow Lodge. Long place. Looks like the kind that doesn't answer.","Harrow again. Nobody's touched that note since Thursday.",""}},
   new PropertyText{Docket="Bell residence",Note="Knock hard. I'm usually in the back\nand I don't always hear the door.\n- Daniel Bell",NoteNight2="Sorry I missed you. Door's open.\nBack room. Leave it on the table.\nThank you for Thursday.\n- Dan Bell  Thurs. Oct 1, 11:40pm",Instructions="Knock hard. If he doesn't answer, papers on the table in the back room.",Inside="Heat's off. Been off a few days, by the feel of it.",Reveal="Something in the dark just got up off the floor.",Death="A county flashlight was found on the Bell drive, still switched on.",Arrival=new[]{"Bell residence. Long drive. Hope he's the type who answers.","Bell's place. I'm not walking that drive slow this time.",""}},
   new PropertyText{Docket="Morrow House",Note="Door's open. Go up and follow the\nlanding to the lamp at the far end.\nEnvelope on that desk is fine.\nWipe your feet. I just did the floors.\n- J. Morrow",NoteNight2="Door's open. Up the stairs again,\nlamp at the far end of the landing.\nI did the floors today. Wipe your\nfeet on the mat. Please. Every time.\n- J. Morrow",Instructions="Upstairs. Follow the landing to the lamp at the far end. Envelope on that desk.",Inside="Floor's still wet. Somebody really did just mop. At this hour.",Reveal="Something's on the gallery. It waited for me to put that down.",Death="The docket was found on the front steps of 108 Latigo, soaked through.",Arrival=new[]{"Morrow House. Mud on the steps. Somebody was here not long ago.","Morrow again. I keep thinking an upstairs window was lit a second ago.",""}},
  };
  public static PropertyText For(int index)=>index>=0&&index<properties.Length?properties[index]:null;
  public static string Note(int index,int night){var p=For(index);if(p==null)return "";return night>0&&!string.IsNullOrEmpty(p.NoteNight2)?p.NoteNight2:p.Note;}
  public static string Arrival(int index,int night){var p=For(index);if(p==null||night<0||night>=p.Arrival.Length)return null;return string.IsNullOrEmpty(p.Arrival[night])?null:p.Arrival[night];}
  static ServiceDialogue.Step S(string line,string[] choices,string[] replies)=>new ServiceDialogue.Step(line,choices,replies);
  public static (string speaker,ServiceDialogue.Step[] steps,string after)? Doorstep(int property,int night){
   switch(property*10+night){
    case 0: return ("Walter Correll",new[]{S("Rex, hush. Hush. ...County? This late it's either taxes or somebody died.",new[]{"Walter Correll? I've just got papers for you.","Sorry about the hour. Won't take a minute.","Somebody ought to do something about that dog."},new[]{"Papers is how both of those start, son.","Hour doesn't matter. I don't sleep much since Ruth passed.","...Somebody ought to. Ruth's dog. He's barked every night since she went. All night. Every night."}),S("It'll be the hospital. Ruth's bills. They were good to her at the end, I'll give them that.",new[]{"I don't know what's in them. I just deliver.","Could be. They don't tell me much."},new[]{"Funny job. Carrying things you haven't read to people who don't want them.","They don't tell anybody much. You find out when it's in your mailbox."}),S("Where've they got you going after me?",new[]{"Harrow Lodge, then out Latigo.","Few more stops out this way."},new[]{"Harrow's. Nobody's answered that door in years. Lights still come on, though. Some nights.","Plenty of houses out this way. Not so many people."}),S("Well. Give it here. I suppose you want a signature.",new[]{"Just at the bottom. Thanks.","Sorry again about the hour."},new[]{"There. Go slow on Latigo. Things come out of those trees. Deer and whatnot.","Don't be. It's nice to get a knock. Even this kind."})},"He takes the envelope and shuts the door. Two bolts, then the chain.");
    case 1: return ("Walter Correll",new[]{S("You again. On a Sunday. Either I'm in trouble or you're lonely.",new[]{"More papers, Mr. Correll. Sorry.","Little of both, maybe.","They don't give us Sundays this month."},new[]{"Don't be sorry. It's not your name on them.","Ha. Well, Rex likes you, anyhow. He only barked twice.","Ruth's brother drove for the county. Roads. They worked him like that till his heart quit."}),S("Heard a car come back by here Thursday. Late. Three, maybe. Sounded like yours. Didn't have its lights on.",new[]{"Wasn't me. I was home by one.","Probably a logging truck.","Did you see who was driving?"},new[]{"Well. Sounded like yours, is all.","Logging trucks got headlights, son.","I didn't go to the window. I'm too old to go to windows."}),S("You see Daniel Bell Thursday? He calls me every Saturday. About nothing, mostly. Didn't call yesterday.",new[]{"He signed. Seemed alright. Quiet.","I didn't hang around after.","I can't really talk about other stops."},new[]{"Quiet's how he is. Still. Saturday's Saturday.","No. I don't expect you did.","No. Course not. He's just never missed a Saturday, is all."}),S("Go on, give it here. And do me a favor, would you.",new[]{"What's that?","Depends on the favor."},new[]{"Don't stop here on your way back tonight. Whatever you hear. Just keep on toward town.","Small one. Don't stop here on your way back. Not for anything. Not if I wave, even."})},"He takes it and shuts the door. I don't hear him walk away from it.");
    case 40: return ("Daniel Bell",new[]{S("Keep it down, would you. ...County? Bit late for the county.",new[]{"Daniel Bell? I've got papers for you.","Sorry. Didn't mean to wake anybody."},new[]{"That's me. Go on, then. Let's see what they want now.","Nobody to wake. It's only me. It's only ever me."}),S("This from Carol's lawyer? She said there'd be more.",new[]{"I don't know. I just deliver them.","It's from the court. That's all I know."},new[]{"Right. Twelve years married, and it's the county that comes to the door.","Doesn't matter. I'll read it in the morning. When it's light out."}),S("You come in off Millbrook? Past Correll's place?",new[]{"Yeah. His dog about took my arm off.","I did. Why?"},new[]{"Rex. He's alright. He barks at the right things.","He have his porch light on? ...Good. That's good."}),S("They've got me down as 'Dan' on here. It's Daniel. Always has been. ...Never mind. Where do I sign?",new[]{"Bottom of the page. Thanks.","I can have the clerk fix the name.","Everything alright in there, Mr. Bell?"},new[]{"There. Go on. Walk the drive quick. It's longer going back than it looks.","Don't bother. They never listen down there. Go on, now. Walk the drive quick.","It's the house. Old brick makes noise in the rain. Go on, now. Don't hang around out front."})},"He takes it without reading it. The door closes. The lock doesn't turn.");
   }
   return null;
  }
  public const string LookAwayHeadline="Stay still";
  public const string LookAwaySurvived="It's stopped. Okay. I'm done here. I'm going.";
  public const string ChaseHeadlineOnFoot="Run";
  public const string ChaseOnFoot="Out. Back to the car. Don't stop.";
  public const string ChaseStartCarHeadline="Start the car";
  public const string ChaseStartCar="Come on. Turn over. Come on, come on.";
  public const string ChaseDriveHeadline="Drive";
  public const string ChaseDrive="Go. Just drive. Don't stop for anything.";
  public const string AtTheCarDoor="Get in. Get in the car.";
  public const string Escaped="Okay. I'm out. I'm not putting that in the log.";
  public const string ReturnAmbush="His door just banged open behind me. Mr. Bell?";
  public const string RetryAtGate="Back at the end of the drive. Papers still in my hand.";
  public const string RetryAtGateServed="Back at the end of the drive. It's done. Get to the car.";
  public const string CarDoorContact="It's at the window. Start it. Start it.";
  public static readonly string[] LookAwayLines=new[]{"Don't turn around. Keep your back to it.","Stand still. Don't even shift your feet.","It's getting quieter. Not yet. Hold."};
  public const string NoAnswer="Nothing. I'll give it a second. ...Nothing.";
  public const string LatchGives="The latch gives. There's a note taped to the door.";
  public const string LatchGivesNoteRead="The latch gives. Unlocked, like the note said.";
  public const string EnvelopeLeft="Papers are down. That counts as served.";
  public const string NoContact="No contact. I'll write it up and move on.";
  public const string KnockFirst="Knock first. Even out here, you knock first.";
  public const string AllVisitsRecorded="That's the docket. Back to the depot.";
  public const string RouteClosedEarly="I'm calling it. The rest can wait. Back to the depot.";
  public const string BarricadeGone="The barricade's gone. No tracks. Just road where it used to be.";
  public const string PastTheSurvey="Odometer's stopped. Probably the cable. I'm still moving. I think.";
  public const string ParcelInside="I stepped over the board that creaks. I didn't know I knew that.";
  public const string Epilogue="Civil process routes in Hollis County were suspended that November, after a process server failed to return from an evening shift.\n\nThe county car was found parked at the depot the next morning, engine running, headlights on. The return of service had been filed and signed.\n\nOne address on that docket, 1 County Route 9, does not appear on any county survey. The road it sits on was closed in 1971.\n\nThe route was never reassigned.";
  public const string DogBarks="Easy. I'm not here for you.";
  public const string LightsChanged="That light. It wasn't like that a minute ago. Timer, probably.";
  public const string OmenUpstairs="Footsteps. Overhead. \"No need to come find me,\" the note said. Fine by me.";
  public const string OmenUpstairsNight2="That was a door. Up there. Hard. Leave it on the desk and go.";
  public const string OmenRadio="...The radio's off. It was on when I came in. Wasn't it?";
  public const string OmenTreeline="There was somebody standing at the treeline. There was. Wasn't there?";
  public const string OmenFog="Fog's coming in off the low ground. Of course it is.";
  public const string NotWhileDriving="Not while I'm driving.";
  public const string ParkAndWalk="Drive's all mud past here. Not getting the car stuck. I'll walk up.";
  public const string OmenValeHall="Somebody was standing at the end of that hall. Just standing there. Looking at me.";
  public const string ChaseWinded="Can't breathe. Keep going. Keep going.";
  public static readonly string[] DepotIntro=new[]{"Five stops. Home by one if the rain lets up. It won't.","Same five addresses. On a Sunday. I almost called in sick.","Two stops tonight. Vale again, and a road I've never driven."};
  public const string ApparitionMorrow="...Mrs. Morrow? ...There was someone at the desk. There was.";
  public const string ApparitionBell="Someone was standing in the hall. Right there. Between me and the door.";
  public const string ApparitionRoad="Somebody on the shoulder. Out here. ...Was there?";
  public const string WalterCallBranch="...Who's that. ...Oh. It's you. Hold on. I'm coming.";
  public const string WalterTurnLine1="Dog's quiet now. You hear that? Quiet.";
  public static readonly string[] WalterTurnChoices=new[]{"Where's Rex?","Mr. Correll... are you all right?"};
  public static readonly string[] WalterTurnReplies=new[]{"Where you said. Somebody did something about him.","Never better. First night I've slept since Ruth."};
  public const string WalterTurnLine2="You said it yourself. Somebody ought to do something. So I did.";
  public const string WalterTurnLine3="Barked all night. Every night. ...Now it's your turn.";
  public const string WalterRun="He's coming out. Get to the car. Now.";
  public const string WalterEscaped="He stopped at the end of the drive. Just stood there in the road, watching me go.";
  public const string EndingWalter="Walter Correll, 81, was found sitting on his front steps on the morning of October 5th with a county envelope unopened in his lap. He told the deputies the dog had barked three nights straight, and that somebody had finally done something about it.\n\nThe county car was found at the end of his drive with the driver's door open and the headlights on.\n\nThe dog was never found. Neither was the process server.";
  public const string WalterArrivalBranch="Correll again. ...No barking tonight. No dog in the yard at all.";
  public const string NerveBroke="I can't - I can't not look.";
  public static readonly string[] PetRex=new[]{"Hey, Rex. Good boy. ...You were only doing your job.","Easy, boy. ...He's shaking. What's got into you tonight?","Still keeping watch, Rex? ...Yeah. Me too."};
  public const string PetRexAgain="Good boy.";
  public const string OmenDoorOpened="...I shut that. I know I shut that.";
  public const string WipedFeet="There. Clean boots. She would notice.";
  public const string WipedFeetAgain="Wiped them. Every step, like she asked.";
  public const string MuddyFloor="...Mud. All over her clean floor. She asked me to wipe my feet.";
  public const string BookFell="...Just a book. Just a book falling off a shelf.";
  public const string StairBook="...A book. Off the landing and down every stair. ...Nobody's up there. Nobody's supposed to be.";
  public const string SecondWipe="...That wasn't me. Somebody else just wiped their feet on her mat.";
  public static string WhosThere(int property,int night){switch(property*10+night){case 0: return "Who's out there? ...Rex! Quiet! ...Hold on, I'm coming.";case 1: return "Who is it? ...It's Sunday, for God's sake. Hold on.";case 40: return "Yeah? Who's there? ...Hang on. Hang on, I'm coming.";}return null;}
  public static string Resident(int property){switch(property){case 0: return "Walter Correll";case 4: return "Daniel Bell";}return "";}
 }
}
