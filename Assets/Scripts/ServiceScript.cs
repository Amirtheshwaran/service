namespace ServiceGameV2 {
 // V19 script: every spoken line, note and inner-voice subtitle. Generated from Sources/V19/script.json; edit there.
 public static class ServiceScript {
  public sealed class PropertyText{public string Docket,Note,NoteNight2,Instructions,Inside,Reveal,Death;public string[] Arrival;}
  static readonly PropertyText[] properties={
   new PropertyText{Docket="Correll residence",Note="",NoteNight2="",Instructions="Knock. Serve Walter Correll in person at the front door.",Inside="",Reveal="",Death="",Arrival=new[]{"Porch light's on. Dog in the yard. Somebody actually lives out here.","Correll again. Rex remembers me. Doesn't like me any better.",""}},
   new PropertyText{Docket="Vale House",Note="Can't come down. Let yourself in -\nand SHUT THE DOOR behind you.\nDon't leave it standing open.\nRight-hand stairs, left at the landing.\nStudy's the first door on the left.\nDesk is fine.\n- M. Vale",NoteNight2="Lamp's on in the study. Desk is fine.\nSHUT THE DOOR behind you. Every time.\nDon't mind the noise upstairs.\nIt's the house.\n- M. Vale",Instructions="Right-hand stairs, left at the landing. The study is the first door on the left. Papers on the desk.",Inside="Hello? County. Just dropping something off. I'll be quick.",Reveal="Something just came out into the hall. It saw me.",Death="Vale House was searched the next morning. The study door was locked.",Arrival=new[]{"Vale House. Too much house for this road. Too many windows.","Vale again. Right stairs, left, first door, desk, out. Don't think about it.","Vale. Third time. ...And after this, an address that hasn't existed for eleven years."}},
   new PropertyText{Docket="Unsurveyed parcel",Note="Come in out of the rain.\nPost on the table past the sitting room.\nYou know the way.\nWe kept the light on for you.\n- Mom",NoteNight2="",Instructions="Through the sitting room. Leave the post on the table beyond it.",Inside="It's warm in here. It smells like Mom's kitchen. ...It can't. It can't smell like anything.",Reveal="Car's running out there. The keys are in my pocket.",Death="",Arrival=new[]{"","","...That's our porch. That's our house. It burned down in '87. I watched it burn. ...It's standing."}},
   new PropertyText{Docket="Harrow Lodge",Note="County - it's not locked. Come in.\nLeave it on the table by the lamp,\nfar end of the hall.\nI leave the lamp on. Always have.\n- E. Harrow",NoteNight2="County - table at the end of the hall.\nThe lamp went out Thursday.\nIf you hear it breathing behind you,\nDON'T TURN AROUND. Stand still.\nIt only wants to be looked at.\n- E. Harrow",Instructions="Down the hall to the lamp at the far end. Papers on the table beside it.",Inside="Smells like nobody's opened a window in here since the fifties.",Reveal="That's breathing. Right behind me. It isn't mine.",Death="Papers were found on the table at Harrow Lodge. The server was not.",Arrival=new[]{"Harrow Lodge. Long place. Looks like the kind that doesn't answer.","Harrow again. Nobody's touched that note since Thursday.",""}},
   new PropertyText{Docket="Bell residence",Note="Knock hard. I'm usually in the back\nand I don't always hear the door.\n- Daniel Bell",NoteNight2="Sorry I missed you. Door's open.\nBack room. Leave it on the table.\nThank you for Thursday.\n- Dan Bell  Thurs. Oct 1, 11:40pm",Instructions="Knock hard. If he doesn't answer, papers on the table in the back room.",Inside="Heat's off. Been off a few days, by the feel of it.",Reveal="Something in the dark just got up off the floor.",Death="A county flashlight was found on the Bell drive, still switched on.",Arrival=new[]{"Bell residence. Long drive. Hope he's the type who answers.","Bell's place. I'm not walking that drive slow this time.",""}},
   new PropertyText{Docket="Morrow House",Note="Door's open. Go up and follow the\nlanding to the lamp at the far end.\nEnvelope on that desk is fine.\nWipe your feet. I just did the floors.\n- J. Morrow",NoteNight2="Door's open. Up the stairs again,\nlamp at the far end of the landing.\nI did the floors today. Wipe your\nfeet on the mat. Please. Every time.\n- J. Morrow",Instructions="Upstairs. Follow the landing to the lamp at the far end. Envelope on that desk.",Inside="Floor's still wet. Somebody really did just mop. At this hour.",Reveal="Something's on the gallery. It waited for me to put that down.",Death="The docket was found on the front steps of 108 Latigo, soaked through.",Arrival=new[]{"Morrow House. Mud on the steps. Somebody was here not long ago.","Morrow again. I keep thinking an upstairs window was lit a second ago.",""}},
  };
  public static PropertyText For(int index)=>index>=0&&index<properties.Length?properties[index]:null;
  public static string Note(int index,int night){var p=For(index);if(p==null)return "";return night>0&&!string.IsNullOrEmpty(p.NoteNight2)?p.NoteNight2:p.Note;}
  public static string Arrival(int index,int night){var p=For(index);if(p==null||night<0||night>=p.Arrival.Length)return null;return string.IsNullOrEmpty(p.Arrival[night])?null:p.Arrival[night];}
  static ServiceDialogue.Step S(string line,string[] choices,string[] replies)=>new ServiceDialogue.Step(line,choices,replies);
  public static (string speaker,ServiceDialogue.Step[] steps,string after)? Doorstep(int property,int night){
   switch(property*10+night){
    case 0: return ("Walter Correll",new[]{S("Rex, hush. Hush. ...County? This late it's either taxes or somebody died.",new[]{"Walter Correll? I've just got papers for you.","Sorry about the hour. Won't take a minute.","Somebody ought to do something about that dog."},new[]{"Papers is how both of those start, son.","Hour doesn't matter. I don't sleep much since Ruth passed.","...Somebody ought to. Ruth's dog. He's barked every night since she went. All night. Every night."}),S("It'll be the hospital. Ruth's bills. They were good to her at the end, I'll give them that.",new[]{"I don't know what's in them. I just deliver.","Could be. They don't tell me much."},new[]{"Funny job. Carrying things you haven't read to people who don't want them.","They don't tell anybody much. You find out when it's in your mailbox."}),S("Where've they got you going after me?",new[]{"Harrow Lodge, then out Latigo.","Few more stops out this way."},new[]{"Harrow's. Nobody's answered that door in years. The old Reverend kept a lamp lit in there forty years - said it kept the Devil's child sitting still. Mother used to say: breathing behind you in there, don't turn around.","Plenty of houses out this way. Not so many people."}),S("Well. Give it here. I suppose you want a signature.",new[]{"Just at the bottom. Thanks.","Sorry again about the hour."},new[]{"There. Go slow on Latigo. Things come out of those trees. ...And out there you shut every door behind you. Every one.","Don't be. It's nice to get a knock. Even this kind."})},"He takes the envelope and shuts the door. Two bolts, then the chain.");
    case 1: return ("Walter Correll",new[]{S("You again. On a Sunday. Either I'm in trouble or you're lonely.",new[]{"More papers, Mr. Correll. Sorry.","Little of both, maybe.","They don't give us Sundays this month."},new[]{"Don't be sorry. It's not your name on them.","Ha. Well, Rex likes you, anyhow. He only barked twice.","Ruth's brother drove for the county. Roads. They worked him like that till his heart quit."}),S("Heard a car come back by here Thursday. Late. Three, maybe. Sounded like yours. Didn't have its lights on.",new[]{"Wasn't me. I was home by one.","Probably a logging truck.","Did you see who was driving?"},new[]{"Well. Sounded like yours, is all.","Logging trucks got headlights, son.","I didn't go to the window. I'm too old to go to windows."}),S("You see Daniel Bell Thursday? He calls me every Saturday. About nothing, mostly. Didn't call yesterday.",new[]{"He signed. Seemed alright. Quiet.","I didn't hang around after.","I can't really talk about other stops."},new[]{"Quiet's how he is. Still. Saturday's Saturday.","No. I don't expect you did.","No. Course not. He's just never missed a Saturday, is all."}),S("Go on, give it here. And do me a favor, would you.",new[]{"What's that?","Depends on the favor."},new[]{"Don't stop here on your way back tonight. Ruth saw the Lowell girl at our gate, the week before Ruth passed. Same gate that child came knocking at in '58. We didn't open it. ...Whatever you hear. Just keep on toward town.","Small one. Don't stop here on your way back. Not if I wave, even. ...Ruth's dog barks at that gate all night. Same gate we kept shut on the Lowell girl."})},"He takes it and shuts the door. I don't hear him walk away from it.");
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
  public const string Epilogue="Civil process routes in Hollis County were suspended that November, after a process server failed to return from an evening shift.\n\nThe county car was found parked at the depot the next morning, engine running, headlights on. The return of service had been filed and signed.\n\nOne address on that docket, 1 County Route 9, does not appear on any county survey. The road it sits on has been closed since the fire of 1987.\n\nThe route was never reassigned.";
  public const string DogBarks="Easy. I'm not here for you.";
  public const string LightsChanged="That window was dark a minute ago. Now there's a lamp on. ...On a timer. Has to be.";
  public const string OmenUpstairs="Footsteps. Overhead. \"Can't come down,\" the note said. ...Fine by me.";
  public const string OmenUpstairsNight2="That was a door. Up there. Hard. Leave it on the desk and go.";
  public const string OmenRadio="...The radio's off. It was on when I came in. Wasn't it?";
  public const string OmenTreeline="There was somebody standing at the treeline. There was. Wasn't there?";
  public const string OmenFog="Fog's coming in off the low ground. Of course it is.";
  public const string NotWhileDriving="Not while I'm driving.";
  public const string ParkAndWalk="Drive's all mud past here. Not getting the car stuck. I'll walk up.";
  public const string OmenValeHall="Somebody was standing at the end of that hall. Just standing there. Looking at me.";
  public const string ChaseWinded="Can't breathe. Keep going. Keep going.";
  public static readonly string[] DepotIntro=new[]{"Five stops. Home by one if the rain lets up. It won't.","Same five addresses. On a Sunday. I almost called in sick.","Two stops tonight. Vale again, and 1 County Route 9. ...That's our address. Route 9's been closed since the fire. Somebody at the county typed it wrong."};
  public const string ApparitionMorrow="Somebody at the desk. ...Two taps on the wood. Sofia used to knock like that on my door when she couldn't sleep. ...I'm tired. That's all.";
  public const string ApparitionBell="That was Sofia. In Bell's hall. Same two taps. ...She can't even sit up anymore.";
  public const string ApparitionRoad="Sofia. On the shoulder, in the rain. ...Standing. She hasn't stood up in a month. ...That wasn't her. Something wearing her.";
  public const string WalterCallBranch="...Who's that. ...Oh. It's you. Hold on. I'm coming.";
  public const string WalterTurnLine1="Dog's quiet now. You hear that? Quiet.";
  public const string ValeKnocksOutside="Three knocks. On the outside of her door. ...Nobody came up the walk.";
  public const string ValeOutsideSeen="It was standing on her walk. Waiting for the door. ...It knows I'm in here.";
  public const string WalterTell="His sleeves are wet to the elbows. ...Don't ask about the dog. Just give him the papers.";
  public static readonly string[] WalterTurnChoices=new[]{"Here. Your papers, Mr. Correll. I'll be going.","Where's Rex, Walter? What did you do?"};
  public static readonly string[] WalterTurnReplies=new[]{"...Papers. Sure. ...Go on, then. Go on, before I think about it.","Where you said. Somebody did something about him."};
  public const string WalterSpared="He takes it without looking at it. The door shuts. No bolt tonight. No chain. ...He's still standing right behind it.";
  public const string WalterTurnLine2="You said it yourself. Somebody ought to do something. So I did.";
  public const string WalterTurnLine3="Barked at that gate all night. Every night. Like he knew what we did. ...Like you know.";
  public const string WalterRun="He's coming out. Get to the car. Now.";
  public const string WalterEscaped="He's stopped. Just standing there in the road, watching me.";
  public const string EndingWalter="Walter Correll, 81, was found sitting on his front steps on the morning of October 5th with a county envelope unopened in his lap. He told the deputies the dog had barked at the gate three nights straight, the way it barked the week the Lowell girl came, and that somebody had finally done something about it.\n\nThe county car was found at the end of his drive with the driver's door open and the headlights on.\n\nThe dog was never found. Neither was the process server.";
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
  public const string SecondWipe="...Something's standing on her mat. Behind me. Don't look. Just get upstairs.";
  public const string Pager0="PAGE - ST. BRIGID'S, 4 WEST\nSofia asking for you again.\nShe says don't work too late.\n- Pat";
  public const string Pager1="PAGE - ST. BRIGID'S, 4 WEST\nRough night Thursday. She keeps\nsaying she saw you out on a road.\nAnd asking who June is. - Pat";
  public const string Pager2="PAGE - ST. BRIGID'S, 4 WEST\nPLEASE CALL 4 WEST.\nURGENT.";
  public const string PagerFinal="PAGE - ST. BRIGID'S, 4 WEST\nSofia passed away at {TIME}.\nWe tried to reach you.\nI'm so sorry. - Pat";
  public const string PagerThought0="Sofia. ...My kid sister, flat on her back on 4 West, telling me not to work late. Five stops, then straight to St. Brigid's.";
  public const string PagerThought1="June. ...I don't know any June. ...Thursday. Morrow's desk. Somebody at the desk, knocking like Sofia.";
  public const string PagerThought2="Urgent. ...Two stops. Then I drive straight there. Two stops.";
  public const string HarrowSteps="Footsteps. Behind me. Coming down the hall. ...Stop. Don't move. Don't turn around.";
  public const string ValeDoorWarn="Cold air on my neck. ...The front door. She said to shut it behind me.";
  public const string ValeDoorShut="Something just hit the door. From outside. ...Okay. It's shut. It's shut.";
  public const string BellFreeze="Don't move. Don't breathe. Don't move.";
  public const string BellPage="If you're reading this, GET OUT, and don't come back.\n\nDad carried her down the cellar stairs at the Lowell place in '58. After that it waited for him on ours. Every night, the same way.\n\nNext time it keeps you.\n\n- D.B.";
  public const string BellPageThought="Bell's writing. ...Get out. Okay. Okay. The papers are down. I'm going.";
  public const string BellPageThoughtPending="Bell's writing. ...Get out. The papers - back room, then out. Don't stop. Don't come back.";
  public const string BellCameBack="He said don't come back. ...It's on the stairs. It was waiting.";
  public const string BellKnockSoft="Nothing. ...'Knock hard,' he wrote. Right.";
  public const string FinaleTable="The post on the table is addressed to me. In my handwriting.";
  public const string FinaleNow="{TIME}. That's now. That's right now.";
  public const string FinaleVoice="A voice, right behind me. Sofia's: \"...You came home.\"";
  public const string EndingMain="Civil process routes in Hollis County were suspended that November, after a process server failed to return from an evening shift.\n\nThe county car was found at the end of County Route 9, a road closed since the Lowell house fire of 1987. The engine was running and the headlights were on. The docket on the passenger seat had been signed for at every stop.\n\nSofia Lowell died at St. Brigid's Hospital at {TIME} on {DATE}, 1998. The night nurse on 4 West told deputies that just before the end Sofia sat up and said her brother had finally come home.\n\nIn October 1958 a nine-year-old girl, June Lowell, went missing from the same house while her parents were at St. Brigid's with her infant brother, Thomas. She was never found. The county's record of the search party lists five households, in this order: Harrow, Vale, Bell, Morrow, Correll.\n\nDaniel Bell, of 91 Latigo Trail, was reported missing the same week. A stack of books was found at the foot of his stairs.\n\nThe route was never reassigned.";
  public static readonly string[] IntroLines=new[]{"Hollis County, 1998.","The county pays triple for night service on the Latigo route. Nobody else will drive it.","Mom and Dad died in the fire out on Route 9 in '87. My little sister Sofia and I were the only ones who got out of that house.","Now she's on 4 West at St. Brigid's, and it's just me. The bills don't stop because I'm tired."};
  public const string ApparitionWhisper="A whisper, right at my ear: \"...don't work too late.\"";
  public const string ReadHarrowPhoto="The frame's backing is loose. Tucked in behind the picture, a snapshot: a church picnic, St. Jude's, summer 1958. Three rows of families in their Sunday clothes, the Reverend in the middle holding his lamp in broad daylight.\n\nAt the end of the front row a little girl has been scratched out with a pin - face, hands, all of her.\n\nOn the back, in pencil:\n'She would not sit still. God forgive us. - E.H.'";
  public const string ThoughtHarrowPhoto="Somebody took a pin to that little girl. ...And he kept a lamp lit for forty years.";
  public const string ReadValeLetter="Maggie -\n\nMother asked again tonight whether the cellar door at the Lowell place was shut. I told her it's been shut forty years. She said then why does she hear it open.\n\nShut every door behind you in that house. Every one. It only comes in where somebody left a door open.\n\n- Aunt Bea";
  public const string ThoughtValeLetter="The Lowell place. ...That's ours. Route 9. Dad nailed the cellar door shut before we moved in. Said the stairs were rotten.";
  public const string ReadMorrowBulletin="ST. JUDE'S LADIES' GUILD - FALL BULLETIN, 1971\n\nIN MEMORY of Agnes Morrow, who kept the church floors so clean you could see the windows in them.\n\n\"You can't get mud out of pine,\" she liked to say, \"but you can try every night.\"\n\nIn pencil, in the margin: Mother scrubbed the Lowell kitchen that night till her hands bled. - J.";
  public const string ThoughtMorrowBulletin="The Lowell kitchen. ...Our kitchen. What did she have to scrub off it? ...And somebody in this house still mops every night.";
  public const string ReadValeClipping="HOLLIS COURIER - THURSDAY, OCTOBER 9, 1958\n\nLOWELL GIRL, 9, STILL MISSING\n\nJune Lowell, nine, of County Route 9, has not been seen since Saturday. Her parents, Henry and Ellen Lowell, were at St. Brigid's Hospital with their infant son, Thomas, when she went missing.\n\nThe Rev. Elias Harrow, who led the search with neighbors from Millbrook Road and Latigo Trail, asks anyone with information to come forward. \"A quiet child,\" he said. \"She could not sit still.\"";
  public const string ThoughtValeClipping="Thomas Lowell. ...That's Dad. Dad had a sister. June. ...He never said one word about her. ...Neighbors from Millbrook and Latigo. My whole route. I've knocked on every door.";
  public const string FinaleSummons="HOLLIS COUNTY\nCIVIL PROCESS\nNOTICE TO APPEAR\n\nIn the matter of JUNE LOWELL, aged nine, who was shut in the cellar of this house on October 4, 1958, and left there.\n\nServed: HARROW, VALE, BELL,\nMORROW, CORRELL.\nEach household has been shown what it did.\n\nLast to be served, by hand:\nthe last of the Lowells.\n\nCome home.";
  public const string FinaleSummonsThought="The last of the Lowells. ...Sofia. And me.";
  public static string WhosThere(int property,int night){switch(property*10+night){case 0: return "Who's out there? ...Rex! Quiet! ...Hold on, I'm coming.";case 1: return "Who is it? ...It's Sunday, for God's sake. Hold on.";case 40: return "Yeah? Who's there? ...Hang on. Hang on, I'm coming.";}return null;}
  public static string Resident(int property){switch(property){case 0: return "Walter Correll";case 4: return "Daniel Bell";}return "";}
 }
}
