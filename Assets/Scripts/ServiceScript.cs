namespace ServiceGameV2 {
 // All V18 spoken lines and inner-voice subtitles. Written text only; no generated voices.
 public static class ServiceScript {
  static ServiceDialogue.Step S(string line,string[] choices=null,string[] replies=null)=>new ServiceDialogue.Step(line,choices,replies);

  public static (string speaker,ServiceDialogue.Step[] steps)? Doorstep(int property,int night){
   if(property==0&&night==0)return ("Walter Correll",new[]{
    S("Evening. You're not the usual fella.",new[]{"Process server, sir. I've got papers for Walter Correll.","Sorry to bother you this late."},new[]{"That'd be me. Hush, Rex.","Late's the only time anybody comes out this far. Hush, Rex."}),
    S("You're doing the whole Latigo run tonight? Stay on the road.",new[]{"Why's that?","I'll be fine."},new[]{"Somebody's been walking the tree line out past Vale's. Big fella. Leather jacket. Doesn't wave back.","That's what the last one said."}),
    S("Well. Here. Give it here.")});
   if(property==0&&night==1)return ("Walter Correll",new[]{
    S("You again. Heard your car go by late the other night.",new[]{"I had a few stops.","Did something happen?"},new[]{"Mm. Something went by after it. Slow. No headlights.","A truck went by after you. Slow. No headlights."}),
    S("Whatever's past Bell's place, you leave it be.",new[]{"I'm just delivering papers.","What's past Bell's place?"},new[]{"That's what I'm afraid of.","Nothing that's on your map."}),
    S("Go on. Before Rex starts again.")});
   if(property==0&&night==2)return ("Walter Correll",new[]{
    S("They added a road out there. I've lived here forty years. There's no road out there.",new[]{"It's on my docket.","Have you seen it?"},new[]{"Then somebody wants you on it.","I've seen the lights. That's enough."})});
   if(property==4&&night==0)return ("Daniel Bell",new[]{
    S("...Help you?",new[]{"Daniel Bell? I have court papers for you.","Evening. Sorry, it's late."},new[]{"Who's asking.","It is."}),
    S("Who sent you all the way out here?",new[]{"The county court.","I just deliver them."},new[]{"The county. Right.","Sure you do."}),
    S("Long drive for a piece of paper. Mind the step on your way out.")});
   return null;
  }

  // Inner voice when you first walk up to an address on a shift.
  public static string Arrival(int property,int night){
   switch(property*10+night){
    case 0: return "Porch light's on. Dog's out. At least somebody's home.";
    case 1: return "Correll again. The dog's quieter tonight.";
    case 30: return "Harrow. No car in the drive. Lights off.";
    case 31: return "It's colder near the door than it should be.";
    case 10: return "Vale House. Big place for somebody who never opens their mail.";
    case 11: return "The front door's already open a crack.";
    case 12: return "Vale again. I shouldn't have come back here.";
    case 40: return "Bell residence. Truck in the drive. Engine's still ticking.";
    case 41: return "Bell's truck is gone. The door isn't.";
    case 50: return "Morrow House. Mud on the steps. Somebody was here recently.";
    case 51: return "Somebody's scraped the notice off the post and put it back.";
    case 22: return "This road isn't on any map I've been given.";
   }
   return null;
  }

  // Night one: nothing supernatural, just an ordinary delivery.
  public static string QuietDelivery(int property)=>property==3?"Envelope's on the table. Nobody home. Good.":property==1?"Left on the desk. The house is dead quiet.":"Left by the lamp. Time to go.";
  public const string TreeLineSeen="Was somebody standing out there?";
  public const string TreeLineMissed="...I could have sworn somebody was standing there.";
  public const string WindshieldNote="A note under the wiper. \"STOP COMING OUT HERE.\"";
 }
}
