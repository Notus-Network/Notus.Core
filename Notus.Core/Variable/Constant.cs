﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using Notus.Variable.Enum;

namespace Notus.Variable
{
    public static class Constant
    {
        public static readonly JsonSerializerOptions JsonSetting = new JsonSerializerOptions() { WriteIndented = true };


        // kaç saniye boyunca pool'u dinleyecek
        public static readonly int WalletMemoryCountLimit = 1000000;

        // kaç saniye boyunca pool'u dinleyecek
        public static readonly int BlockListeningForPoolTime = 200;

        // node kaç milisaniye çalışacak
        public static readonly int BlockGeneratingTime = 100;

        // node çalışma süresi sonunda kaç mili saniye dağıtmaya geçecek
        public static readonly int BlockDistributingTime = 200;

        public static readonly byte RegenerateNodeQueueCount = 4;


        public static readonly string NetworkProgramWallet = "111111111111111111111111111111111111111";
        public static readonly int BlockTransactionLimit = 1000;
        
        //node'ların sıralama frekansları - saniye cinsinden
        public static readonly ulong NodeStartingSync = 20;
        public static readonly ulong NodeSortFrequency = 2;

        // wallet constant
        public static readonly int WalletEncodeTextLength = 36;
        public static readonly int SingleWalletTextLength = 39;
        public static readonly int MultiWalletTextLength = 39;
        public static readonly string SingleWalletPrefix_MainNetwork = "NTS";
        public static readonly string SingleWalletPrefix_TestNetwork = "not";
        public static readonly string SingleWalletPrefix_DevelopmentNetwork = "NOD";

        public static readonly string MultiWalletPrefix_MainNetwork = "NMR";
        public static readonly string MultiWalletPrefix_TestNetwork = "NMT";
        public static readonly string MultiWalletPrefix_DevelopmentNetwork = "NMD";
        public static readonly int MultiWalletTransactionTimeout= 604800;

        //notus coin
        public static readonly string MainCoinTagName = "NOTUS";

        //date time
        public static readonly DateTime DefaultTime = new DateTime(2000, 01, 1, 0, 00, 00);
        public static readonly string DefaultDateTimeFormatText = "yyyyMMddHHmmssfff";
        public static readonly string DefaultDateTimeFormatTextWithourMiliSecond = "yyyyMMddHHmmss";

        //alphabet constant
        public static readonly string DefaultHexAlphabetString = "0123456789abcdef";
        public static readonly string DefaultBase32AlphabetString = "QAZ2WSX3EDC4RFV5TGB6YHN7UJM8K9LP";
        public static readonly string DefaultBase35AlphabetString = "123456789abcdefghijklmnopqrstuvwxyz";
        public static readonly string DefaultBase58AlphabetString = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        public static readonly string DefaultBase64AlphabetString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" + "abcdefghijklmnopqrstuvwxyz" + "0123456789" + "+/";
        public static readonly char[] DefaultBase64AlphabetCharArray = new char[64] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/' };

        public static readonly string[] Bip39WordArray = new string[] { "abandon", "ability", "able", "about", "above", "absent", "absorb", "abstract", "absurd", "abuse", "access", "accident", "account", "accuse", "achieve", "acid", "acoustic", "acquire", "across", "act", "action", "actor", "actress", "actual", "adapt", "add", "addict", "address", "adjust", "admit", "adult", "advance", "advice", "aerobic", "affair", "afford", "afraid", "again", "age", "agent", "agree", "ahead", "aim", "air", "airport", "aisle", "alarm", "album", "alcohol", "alert", "alien", "all", "alley", "allow", "almost", "alone", "alpha", "already", "also", "alter", "always", "amateur", "amazing", "among", "amount", "amused", "analyst", "anchor", "ancient", "anger", "angle", "angry", "animal", "ankle", "announce", "annual", "another", "answer", "antenna", "antique", "anxiety", "any", "apart", "apology", "appear", "apple", "approve", "april", "arch", "arctic", "area", "arena", "argue", "arm", "armed", "armor", "army", "around", "arrange", "arrest", "arrive", "arrow", "art", "artefact", "artist", "artwork", "ask", "aspect", "assault", "asset", "assist", "assume", "asthma", "athlete", "atom", "attack", "attend", "attitude", "attract", "auction", "audit", "august", "aunt", "author", "auto", "autumn", "average", "avocado", "avoid", "awake", "aware", "away", "awesome", "awful", "awkward", "axis", "baby", "bachelor", "bacon", "badge", "bag", "balance", "balcony", "ball", "bamboo", "banana", "banner", "bar", "barely", "bargain", "barrel", "base", "basic", "basket", "battle", "beach", "bean", "beauty", "because", "become", "beef", "before", "begin", "behave", "behind", "believe", "below", "belt", "bench", "benefit", "best", "betray", "better", "between", "beyond", "bicycle", "bid", "bike", "bind", "biology", "bird", "birth", "bitter", "black", "blade", "blame", "blanket", "blast", "bleak", "bless", "blind", "blood", "blossom", "blouse", "blue", "blur", "blush", "board", "boat", "body", "boil", "bomb", "bone", "bonus", "book", "boost", "border", "boring", "borrow", "boss", "bottom", "bounce", "box", "boy", "bracket", "brain", "brand", "brass", "brave", "bread", "breeze", "brick", "bridge", "brief", "bright", "bring", "brisk", "broccoli", "broken", "bronze", "broom", "brother", "brown", "brush", "bubble", "buddy", "budget", "buffalo", "build", "bulb", "bulk", "bullet", "bundle", "bunker", "burden", "burger", "burst", "bus", "business", "busy", "butter", "buyer", "buzz", "cabbage", "cabin", "cable", "cactus", "cage", "cake", "call", "calm", "camera", "camp", "can", "canal", "cancel", "candy", "cannon", "canoe", "canvas", "canyon", "capable", "capital", "captain", "car", "carbon", "card", "cargo", "carpet", "carry", "cart", "case", "cash", "casino", "castle", "casual", "cat", "catalog", "catch", "category", "cattle", "caught", "cause", "caution", "cave", "ceiling", "celery", "cement", "census", "century", "cereal", "certain", "chair", "chalk", "champion", "change", "chaos", "chapter", "charge", "chase", "chat", "cheap", "check", "cheese", "chef", "cherry", "chest", "chicken", "chief", "child", "chimney", "choice", "choose", "chronic", "chuckle", "chunk", "churn", "cigar", "cinnamon", "circle", "citizen", "city", "civil", "claim", "clap", "clarify", "claw", "clay", "clean", "clerk", "clever", "click", "client", "cliff", "climb", "clinic", "clip", "clock", "clog", "close", "cloth", "cloud", "clown", "club", "clump", "cluster", "clutch", "coach", "coast", "coconut", "code", "coffee", "coil", "coin", "collect", "color", "column", "combine", "come", "comfort", "comic", "common", "company", "concert", "conduct", "confirm", "congress", "connect", "consider", "control", "convince", "cook", "cool", "copper", "copy", "coral", "core", "corn", "correct", "cost", "cotton", "couch", "country", "couple", "course", "cousin", "cover", "coyote", "crack", "cradle", "craft", "cram", "crane", "crash", "crater", "crawl", "crazy", "cream", "credit", "creek", "crew", "cricket", "crime", "crisp", "critic", "crop", "cross", "crouch", "crowd", "crucial", "cruel", "cruise", "crumble", "crunch", "crush", "cry", "crystal", "cube", "culture", "cup", "cupboard", "curious", "current", "curtain", "curve", "cushion", "custom", "cute", "cycle", "dad", "damage", "damp", "dance", "danger", "daring", "dash", "daughter", "dawn", "day", "deal", "debate", "debris", "decade", "december", "decide", "decline", "decorate", "decrease", "deer", "defense", "define", "defy", "degree", "delay", "deliver", "demand", "demise", "denial", "dentist", "deny", "depart", "depend", "deposit", "depth", "deputy", "derive", "describe", "desert", "design", "desk", "despair", "destroy", "detail", "detect", "develop", "device", "devote", "diagram", "dial", "diamond", "diary", "dice", "diesel", "diet", "differ", "digital", "dignity", "dilemma", "dinner", "dinosaur", "direct", "dirt", "disagree", "discover", "disease", "dish", "dismiss", "disorder", "display", "distance", "divert", "divide", "divorce", "dizzy", "doctor", "document", "dog", "doll", "dolphin", "domain", "donate", "donkey", "donor", "door", "dose", "double", "dove", "draft", "dragon", "drama", "drastic", "draw", "dream", "dress", "drift", "drill", "drink", "drip", "drive", "drop", "drum", "dry", "duck", "dumb", "dune", "during", "dust", "dutch", "duty", "dwarf", "dynamic", "eager", "eagle", "early", "earn", "earth", "easily", "east", "easy", "echo", "ecology", "economy", "edge", "edit", "educate", "effort", "egg", "eight", "either", "elbow", "elder", "electric", "elegant", "element", "elephant", "elevator", "elite", "else", "embark", "embody", "embrace", "emerge", "emotion", "employ", "empower", "empty", "enable", "enact", "end", "endless", "endorse", "enemy", "energy", "enforce", "engage", "engine", "enhance", "enjoy", "enlist", "enough", "enrich", "enroll", "ensure", "enter", "entire", "entry", "envelope", "episode", "equal", "equip", "era", "erase", "erode", "erosion", "error", "erupt", "escape", "essay", "essence", "estate", "eternal", "ethics", "evidence", "evil", "evoke", "evolve", "exact", "example", "excess", "exchange", "excite", "exclude", "excuse", "execute", "exercise", "exhaust", "exhibit", "exile", "exist", "exit", "exotic", "expand", "expect", "expire", "explain", "expose", "express", "extend", "extra", "eye", "eyebrow", "fabric", "face", "faculty", "fade", "faint", "faith", "fall", "false", "fame", "family", "famous", "fan", "fancy", "fantasy", "farm", "fashion", "fat", "fatal", "father", "fatigue", "fault", "favorite", "feature", "february", "federal", "fee", "feed", "feel", "female", "fence", "festival", "fetch", "fever", "few", "fiber", "fiction", "field", "figure", "file", "film", "filter", "final", "find", "fine", "finger", "finish", "fire", "firm", "first", "fiscal", "fish", "fit", "fitness", "fix", "flag", "flame", "flash", "flat", "flavor", "flee", "flight", "flip", "float", "flock", "floor", "flower", "fluid", "flush", "fly", "foam", "focus", "fog", "foil", "fold", "follow", "food", "foot", "force", "forest", "forget", "fork", "fortune", "forum", "forward", "fossil", "foster", "found", "fox", "fragile", "frame", "frequent", "fresh", "friend", "fringe", "frog", "front", "frost", "frown", "frozen", "fruit", "fuel", "fun", "funny", "furnace", "fury", "future", "gadget", "gain", "galaxy", "gallery", "game", "gap", "garage", "garbage", "garden", "garlic", "garment", "gas", "gasp", "gate", "gather", "gauge", "gaze", "general", "genius", "genre", "gentle", "genuine", "gesture", "ghost", "giant", "gift", "giggle", "ginger", "giraffe", "girl", "give", "glad", "glance", "glare", "glass", "glide", "glimpse", "globe", "gloom", "glory", "glove", "glow", "glue", "goat", "goddess", "gold", "good", "goose", "gorilla", "gospel", "gossip", "govern", "gown", "grab", "grace", "grain", "grant", "grape", "grass", "gravity", "great", "green", "grid", "grief", "grit", "grocery", "group", "grow", "grunt", "guard", "guess", "guide", "guilt", "guitar", "gun", "gym", "habit", "hair", "half", "hammer", "hamster", "hand", "happy", "harbor", "hard", "harsh", "harvest", "hat", "have", "hawk", "hazard", "head", "health", "heart", "heavy", "hedgehog", "height", "hello", "helmet", "help", "hen", "hero", "hidden", "high", "hill", "hint", "hip", "hire", "history", "hobby", "hockey", "hold", "hole", "holiday", "hollow", "home", "honey", "hood", "hope", "horn", "horror", "horse", "hospital", "host", "hotel", "hour", "hover", "hub", "huge", "human", "humble", "humor", "hundred", "hungry", "hunt", "hurdle", "hurry", "hurt", "husband", "hybrid", "ice", "icon", "idea", "identify", "idle", "ignore", "ill", "illegal", "illness", "image", "imitate", "immense", "immune", "impact", "impose", "improve", "impulse", "inch", "include", "income", "increase", "index", "indicate", "indoor", "industry", "infant", "inflict", "inform", "inhale", "inherit", "initial", "inject", "injury", "inmate", "inner", "innocent", "input", "inquiry", "insane", "insect", "inside", "inspire", "install", "intact", "interest", "into", "invest", "invite", "involve", "iron", "island", "isolate", "issue", "item", "ivory", "jacket", "jaguar", "jar", "jazz", "jealous", "jeans", "jelly", "jewel", "job", "join", "joke", "journey", "joy", "judge", "juice", "jump", "jungle", "junior", "junk", "just", "kangaroo", "keen", "keep", "ketchup", "key", "kick", "kid", "kidney", "kind", "kingdom", "kiss", "kit", "kitchen", "kite", "kitten", "kiwi", "knee", "knife", "knock", "know", "lab", "label", "labor", "ladder", "lady", "lake", "lamp", "language", "laptop", "large", "later", "latin", "laugh", "laundry", "lava", "law", "lawn", "lawsuit", "layer", "lazy", "leader", "leaf", "learn", "leave", "lecture", "left", "leg", "legal", "legend", "leisure", "lemon", "lend", "length", "lens", "leopard", "lesson", "letter", "level", "liar", "liberty", "library", "license", "life", "lift", "light", "like", "limb", "limit", "link", "lion", "liquid", "list", "little", "live", "lizard", "load", "loan", "lobster", "local", "lock", "logic", "lonely", "long", "loop", "lottery", "loud", "lounge", "love", "loyal", "lucky", "luggage", "lumber", "lunar", "lunch", "luxury", "lyrics", "machine", "mad", "magic", "magnet", "maid", "mail", "main", "major", "make", "mammal", "man", "manage", "mandate", "mango", "mansion", "manual", "maple", "marble", "march", "margin", "marine", "market", "marriage", "mask", "mass", "master", "match", "material", "math", "matrix", "matter", "maximum", "maze", "meadow", "mean", "measure", "meat", "mechanic", "medal", "media", "melody", "melt", "member", "memory", "mention", "menu", "mercy", "merge", "merit", "merry", "mesh", "message", "metal", "method", "middle", "midnight", "milk", "million", "mimic", "mind", "minimum", "minor", "minute", "miracle", "mirror", "misery", "miss", "mistake", "mix", "mixed", "mixture", "mobile", "model", "modify", "mom", "moment", "monitor", "monkey", "monster", "month", "moon", "moral", "more", "morning", "mosquito", "mother", "motion", "motor", "mountain", "mouse", "move", "movie", "much", "muffin", "mule", "multiply", "muscle", "museum", "mushroom", "music", "must", "mutual", "myself", "mystery", "myth", "naive", "name", "napkin", "narrow", "nasty", "nation", "nature", "near", "neck", "need", "negative", "neglect", "neither", "nephew", "nerve", "nest", "net", "network", "neutral", "never", "news", "next", "nice", "night", "noble", "noise", "nominee", "noodle", "normal", "north", "nose", "notable", "note", "nothing", "notice", "novel", "now", "nuclear", "number", "nurse", "nut", "oak", "obey", "object", "oblige", "obscure", "observe", "obtain", "obvious", "occur", "ocean", "october", "odor", "off", "offer", "office", "often", "oil", "okay", "old", "olive", "olympic", "omit", "once", "one", "onion", "online", "only", "open", "opera", "opinion", "oppose", "option", "orange", "orbit", "orchard", "order", "ordinary", "organ", "orient", "original", "orphan", "ostrich", "other", "outdoor", "outer", "output", "outside", "oval", "oven", "over", "own", "owner", "oxygen", "oyster", "ozone", "pact", "paddle", "page", "pair", "palace", "palm", "panda", "panel", "panic", "panther", "paper", "parade", "parent", "park", "parrot", "party", "pass", "patch", "path", "patient", "patrol", "pattern", "pause", "pave", "payment", "peace", "peanut", "pear", "peasant", "pelican", "pen", "penalty", "pencil", "people", "pepper", "perfect", "permit", "person", "pet", "phone", "photo", "phrase", "physical", "piano", "picnic", "picture", "piece", "pig", "pigeon", "pill", "pilot", "pink", "pioneer", "pipe", "pistol", "pitch", "pizza", "place", "planet", "plastic", "plate", "play", "please", "pledge", "pluck", "plug", "plunge", "poem", "poet", "point", "polar", "pole", "police", "pond", "pony", "pool", "popular", "portion", "position", "possible", "post", "potato", "pottery", "poverty", "powder", "power", "practice", "praise", "predict", "prefer", "prepare", "present", "pretty", "prevent", "price", "pride", "primary", "print", "priority", "prison", "private", "prize", "problem", "process", "produce", "profit", "program", "project", "promote", "proof", "property", "prosper", "protect", "proud", "provide", "public", "pudding", "pull", "pulp", "pulse", "pumpkin", "punch", "pupil", "puppy", "purchase", "purity", "purpose", "purse", "push", "put", "puzzle", "pyramid", "quality", "quantum", "quarter", "question", "quick", "quit", "quiz", "quote", "rabbit", "raccoon", "race", "rack", "radar", "radio", "rail", "rain", "raise", "rally", "ramp", "ranch", "random", "range", "rapid", "rare", "rate", "rather", "raven", "raw", "razor", "ready", "real", "reason", "rebel", "rebuild", "recall", "receive", "recipe", "record", "recycle", "reduce", "reflect", "reform", "refuse", "region", "regret", "regular", "reject", "relax", "release", "relief", "rely", "remain", "remember", "remind", "remove", "render", "renew", "rent", "reopen", "repair", "repeat", "replace", "report", "require", "rescue", "resemble", "resist", "resource", "response", "result", "retire", "retreat", "return", "reunion", "reveal", "review", "reward", "rhythm", "rib", "ribbon", "rice", "rich", "ride", "ridge", "rifle", "right", "rigid", "ring", "riot", "ripple", "risk", "ritual", "rival", "river", "road", "roast", "robot", "robust", "rocket", "romance", "roof", "rookie", "room", "rose", "rotate", "rough", "round", "route", "royal", "rubber", "rude", "rug", "rule", "run", "runway", "rural", "sad", "saddle", "sadness", "safe", "sail", "salad", "salmon", "salon", "salt", "salute", "same", "sample", "sand", "satisfy", "satoshi", "sauce", "sausage", "save", "say", "scale", "scan", "scare", "scatter", "scene", "scheme", "school", "science", "scissors", "scorpion", "scout", "scrap", "screen", "script", "scrub", "sea", "search", "season", "seat", "second", "secret", "section", "security", "seed", "seek", "segment", "select", "sell", "seminar", "senior", "sense", "sentence", "series", "service", "session", "settle", "setup", "seven", "shadow", "shaft", "shallow", "share", "shed", "shell", "sheriff", "shield", "shift", "shine", "ship", "shiver", "shock", "shoe", "shoot", "shop", "short", "shoulder", "shove", "shrimp", "shrug", "shuffle", "shy", "sibling", "sick", "side", "siege", "sight", "sign", "silent", "silk", "silly", "silver", "similar", "simple", "since", "sing", "siren", "sister", "situate", "six", "size", "skate", "sketch", "ski", "skill", "skin", "skirt", "skull", "slab", "slam", "sleep", "slender", "slice", "slide", "slight", "slim", "slogan", "slot", "slow", "slush", "small", "smart", "smile", "smoke", "smooth", "snack", "snake", "snap", "sniff", "snow", "soap", "soccer", "social", "sock", "soda", "soft", "solar", "soldier", "solid", "solution", "solve", "someone", "song", "soon", "sorry", "sort", "soul", "sound", "soup", "source", "south", "space", "spare", "spatial", "spawn", "speak", "special", "speed", "spell", "spend", "sphere", "spice", "spider", "spike", "spin", "spirit", "split", "spoil", "sponsor", "spoon", "sport", "spot", "spray", "spread", "spring", "spy", "square", "squeeze", "squirrel", "stable", "stadium", "staff", "stage", "stairs", "stamp", "stand", "start", "state", "stay", "steak", "steel", "stem", "step", "stereo", "stick", "still", "sting", "stock", "stomach", "stone", "stool", "story", "stove", "strategy", "street", "strike", "strong", "struggle", "student", "stuff", "stumble", "style", "subject", "submit", "subway", "success", "such", "sudden", "suffer", "sugar", "suggest", "suit", "summer", "sun", "sunny", "sunset", "super", "supply", "supreme", "sure", "surface", "surge", "surprise", "surround", "survey", "suspect", "sustain", "swallow", "swamp", "swap", "swarm", "swear", "sweet", "swift", "swim", "swing", "switch", "sword", "symbol", "symptom", "syrup", "system", "table", "tackle", "tag", "tail", "talent", "talk", "tank", "tape", "target", "task", "taste", "tattoo", "taxi", "teach", "team", "tell", "ten", "tenant", "tennis", "tent", "term", "test", "text", "thank", "that", "theme", "then", "theory", "there", "they", "thing", "this", "thought", "three", "thrive", "throw", "thumb", "thunder", "ticket", "tide", "tiger", "tilt", "timber", "time", "tiny", "tip", "tired", "tissue", "title", "toast", "tobacco", "today", "toddler", "toe", "together", "toilet", "token", "tomato", "tomorrow", "tone", "tongue", "tonight", "tool", "tooth", "top", "topic", "topple", "torch", "tornado", "tortoise", "toss", "total", "tourist", "toward", "tower", "town", "toy", "track", "trade", "traffic", "tragic", "train", "transfer", "trap", "trash", "travel", "tray", "treat", "tree", "trend", "trial", "tribe", "trick", "trigger", "trim", "trip", "trophy", "trouble", "truck", "true", "truly", "trumpet", "trust", "truth", "try", "tube", "tuition", "tumble", "tuna", "tunnel", "turkey", "turn", "turtle", "twelve", "twenty", "twice", "twin", "twist", "two", "type", "typical", "ugly", "umbrella", "unable", "unaware", "uncle", "uncover", "under", "undo", "unfair", "unfold", "unhappy", "uniform", "unique", "unit", "universe", "unknown", "unlock", "until", "unusual", "unveil", "update", "upgrade", "uphold", "upon", "upper", "upset", "urban", "urge", "usage", "use", "used", "useful", "useless", "usual", "utility", "vacant", "vacuum", "vague", "valid", "valley", "valve", "van", "vanish", "vapor", "various", "vast", "vault", "vehicle", "velvet", "vendor", "venture", "venue", "verb", "verify", "version", "very", "vessel", "veteran", "viable", "vibrant", "vicious", "victory", "video", "view", "village", "vintage", "violin", "virtual", "virus", "visa", "visit", "visual", "vital", "vivid", "vocal", "voice", "void", "volcano", "volume", "vote", "voyage", "wage", "wagon", "wait", "walk", "wall", "walnut", "want", "warfare", "warm", "warrior", "wash", "wasp", "waste", "water", "wave", "way", "wealth", "weapon", "wear", "weasel", "weather", "web", "wedding", "weekend", "weird", "welcome", "west", "wet", "whale", "what", "wheat", "wheel", "when", "where", "whip", "whisper", "wide", "width", "wife", "wild", "will", "win", "window", "wine", "wing", "wink", "winner", "winter", "wire", "wisdom", "wise", "wish", "witness", "wolf", "woman", "wonder", "wood", "wool", "word", "work", "world", "worry", "worth", "wrap", "wreck", "wrestle", "wrist", "write", "wrong", "yard", "year", "yellow", "you", "young", "youth", "zebra", "zero", "zone", "zoo" };

        public readonly static Dictionary<char, byte> DefaultHexMapCharDictionary = new Dictionary<char, byte>()
        {
        { 'a', 0xA },{ 'b', 0xB },{ 'c', 0xC },{ 'd', 0xD },
        { 'e', 0xE },{ 'f', 0xF },{ 'A', 0xA },{ 'B', 0xB },
        { 'C', 0xC },{ 'D', 0xD },{ 'E', 0xE },{ 'F', 0xF },
        { '0', 0x0 },{ '1', 0x1 },{ '2', 0x2 },{ '3', 0x3 },
        { '4', 0x4 },{ '5', 0x5 },{ '6', 0x6 },{ '7', 0x7 },
        { '8', 0x8 },{ '9', 0x9 }
        };
        public static readonly string CommonDelimeterChar = ":";

        public static readonly string GenesisBlockUid = "100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";

        //socket-exception
        //public static readonly int DefaultMessagePortNo = 5100;
        
        private static readonly int DefaultPortNo = 5000;
        public static readonly int DefaultChunkSize = 2048;

        public static readonly Dictionary<NetworkLayer, Dictionary<NetworkType, string>> AirDropVolume =
            new Dictionary<NetworkLayer, Dictionary<NetworkType, string>>()
        {
            {
                NetworkLayer.Layer1, new Dictionary<NetworkType,string>(){
                    { NetworkType.MainNet, "2000000000"},
                    { NetworkType.TestNet, "2000000000"},
                    { NetworkType.DevNet, "2000000000"}
                }
            }
        };

        public static readonly int MinimumNodeCount = 2;
        public static readonly int TimeSyncCommPort = 27000;

        public static readonly string TimeSyncNodeIpAddress = "89.252.134.111";
        public static readonly int TimeSyncAddingCommPort = 25000;

        // layer 1 - main layer for crypto & token generate and transfer
        public static readonly Dictionary<NetworkLayer, Dictionary<NetworkType, int>> PortNo = new Dictionary<NetworkLayer, Dictionary<NetworkType, int>>()
        {
            {
                NetworkLayer.Layer1, new Dictionary<NetworkType,int>(){
                    { NetworkType.MainNet, DefaultPortNo + 0},
                    { NetworkType.TestNet, DefaultPortNo + 1},
                    { NetworkType.DevNet, DefaultPortNo + 2}
                }
            },
            {
                NetworkLayer.Layer2, new Dictionary<NetworkType,int>(){
                    { NetworkType.MainNet, DefaultPortNo + 100},
                    { NetworkType.TestNet, DefaultPortNo + 101},
                    { NetworkType.DevNet, DefaultPortNo + 102}
                }
            },
            {
                NetworkLayer.Layer3, new Dictionary<NetworkType,int>(){
                    { NetworkType.MainNet, DefaultPortNo + 200},
                    { NetworkType.TestNet, DefaultPortNo + 201},
                    { NetworkType.DevNet, DefaultPortNo + 202}
                }
            },
            {
                NetworkLayer.Layer4, new Dictionary<NetworkType,int>(){
                    { NetworkType.MainNet, DefaultPortNo + 300},
                    { NetworkType.TestNet, DefaultPortNo + 301},
                    { NetworkType.DevNet, DefaultPortNo + 302}
                }
            },
            {
                NetworkLayer.Layer5, new Dictionary<NetworkType,int>(){
                    { NetworkType.MainNet, DefaultPortNo + 400},
                    { NetworkType.TestNet, DefaultPortNo + 401},
                    { NetworkType.DevNet, DefaultPortNo + 402}
                }
            },
            {
                NetworkLayer.Layer6, new Dictionary<NetworkType,int>(){
                    { NetworkType.MainNet, DefaultPortNo + 500},
                    { NetworkType.TestNet, DefaultPortNo + 501},
                    { NetworkType.DevNet, DefaultPortNo + 502}
                }
            },
            {
                NetworkLayer.Layer7, new Dictionary<NetworkType,int>(){
                    { NetworkType.MainNet, DefaultPortNo + 600},
                    { NetworkType.TestNet, DefaultPortNo + 601},
                    { NetworkType.DevNet, DefaultPortNo + 602}
                }
            },
            {
                NetworkLayer.Layer8, new Dictionary<NetworkType,int>(){
                    { NetworkType.MainNet, DefaultPortNo + 700},
                    { NetworkType.TestNet, DefaultPortNo + 701},
                    { NetworkType.DevNet, DefaultPortNo + 702}
                }
            },
            {
                NetworkLayer.Layer9, new Dictionary<NetworkType,int>(){
                    { NetworkType.MainNet, DefaultPortNo + 800},
                    { NetworkType.TestNet, DefaultPortNo + 801},
                    { NetworkType.DevNet, DefaultPortNo + 802}
                }
            },
            {
                NetworkLayer.Layer10, new Dictionary<NetworkType,int>(){
                    { NetworkType.MainNet, DefaultPortNo + 900},
                    { NetworkType.TestNet, DefaultPortNo + 901},
                    { NetworkType.DevNet, DefaultPortNo + 902}
                }
            }
        };

        public static Dictionary<NetworkLayer, string> LayerText = new Dictionary<NetworkLayer, string>() {
            { NetworkLayer.Layer1,"Layer 1 ( Crypto Layer )" },
            { NetworkLayer.Layer2,"Layer 2 ( File Storage Layer )" },
            { NetworkLayer.Layer3,"Layer 3 ( Crypto Message Layer )" },
            { NetworkLayer.Layer4,"Layer 4 ( Secure File Storage Layer )" },
        };
        public static Dictionary<NetworkType, string> DefaultNetworkUrl = new Dictionary<NetworkType, string>() {
            { NetworkType.MainNet,"mainnet.notus.network" },
            { NetworkType.TestNet,"testnet.notus.network" },
            { NetworkType.DevNet,"devnet.notus.network" }
        };
        
        /*
        standart bizim sunucularımız
        */
        public static readonly List<string> ListMainNodeIp = new List<string> {
            "89.252.134.91",
            "89.252.184.151"
        };

        /*
        aws üzerindeki sunucular
        public static readonly List<string> ListMainNodeIp = new List<string> {
            "3.75.110.186",
            "13.229.56.127"
        };
        */

        public const string Default_EccCurveName = "prime256v1";
        public const int Default_WordListArrayCount = 16;

        public static readonly int BlockStorageMonthlyGroupCount = 50;

        public static readonly string SocketMessageEndingText = "<EOF>";
        public static readonly string NonceDelimeterChar = ":";
        public static readonly bool SubFromBiggestNumber = false;

        public readonly static Dictionary<string, string> AlphabetForAccountType = new Dictionary<string, string>()
        {
            { "m", "865b1oxj34cf2k7nhlqtwysrzmigapud9ve" },
            { "i", "b7n3lqt2azvs6xdmchfkojir4pue95wyg18" },
            { "p", "u45il9f2bpvc8s1eorxjndwmthakg3qyz76" },
            { "s", "5lsv3hknew49btqj17fr6ycxpa8doi2mzug" },
            { "c", "m51z3lkjhnabyew48dprs2oi6vcf9gutqx7" },
            { "g", "5ro6ynea4bzx7d9ipuhkfc2w1qg8svlmjt3" },
            { "a", "imdzafk4w8oqbx671utjysph9lec2vnrg35" },
            { "u", "hmtz2fp3ykdie17o4v96ubnsg5xwcajrq8l" }
        };


        // public readonly static string MemoryPool_BlockListDirectoryName = "lists";
        public readonly static int MemoryPool_BlockListCount = 100000;

        public readonly static Dictionary<string, string> MemoryPoolName = new Dictionary<string, string>()
        {
            //{ "MainNetworkSettings", "mns" },
            //{ "MainBlockSettings", "mbs" },
            { "CommonData", "data" },
            { "MasterList", "master_list" },

            { "MainNodeWalletConfig", "node_config" },
            { "BlockPoolList", "pool_list" },
            { "PreviousBlockList", "prev_list" },

            //{ "BlockStorageList", "block_list" },
            { "MempoolListBeforeBlockStorage", "block_file" },

            { "NetworkNodeList", "node_list" }
        };

        public readonly static Dictionary<int, int> NonceHashLength = new Dictionary<int, int>()
        {
            { 1, 32 },      // md5
            { 2, 40 },      // sha1
            { 100, 240 }    // sasha
        };



        // which hash algorithm
        public readonly static int Default_BlockNonceMethod = 1;
        public readonly static Dictionary<int, int> BlockNonceMethod = new Dictionary<int, int>()
        {
            { 360, 100 }
        };

        // how calculate nonce number
        // 1-Slide, 2-Bounce
        public readonly static int Default_BlockNonceType = 1;
        public readonly static Dictionary<int, int> BlockNonceType = new Dictionary<int, int>()
        {
            { 360, 1 }
        };

        // block difficulty level
        public readonly static int Default_BlockDifficulty = 1;
        public readonly static Dictionary<int, int> BlockDifficulty = new Dictionary<int, int>()
        {
            { 360, 1 }
        };

        public readonly static List<int> BalanceBlockTypeList = new List<int>()
        {
            360, 120
        };


        public static readonly int JoinNetworkEndOfTheCounter = 3;

        public static readonly string Seed_ForMainNet_BlockKeyGenerate = "NotusMainNetSeedText";


        public class StorageFolderName
        {
            public const string BlockForTgz = "blocks_tgz";
            public const string Block = "blocks";
            public const string Balance = "balance";
            public const string Node = "node";
            public const string Common = "common";
            public const string File = "file";
            public const string Storage = "storage";
            public const string TempBlock = "temp_block";
            public const string Pool = "pool";
        }


        public class ErrorNoList
        {
            public const int Success = 0;
            public const int AddedToQueue = 1;
            public const int UnknownError = 1;
            public const int NeedCoin = 5;
            public const int AccountDoesntExist = 7;
            public const int WrongSign = 9;
            public const int TagExists = 10;
            public const int WrongAccount = 13;
            public const int MissingArgument = 11;
        }

        public class ContentTypes
        {
            // Text
            public const string Plain = "text/plain";
            public const string Html = "text/html";
            public const string Xml = "text/xml";
            public const string RichText = "text/richtext";
            // Image
            public const string Gif = "image/gif";
            public const string Tiff = "image/tiff";
            public const string Jpeg = "image/jpeg";
            // Application
            public const string Soap = "application/soap+xml";
            public const string Octet = "application/octet-stream";
            public const string Pdf = "application/pdf";
            public const string Zip = "application/zip";
            public const string Json = "application/json";
            public const string ApplicationXml = "application/xml";
        }


        public class MempoolNameList
        {
            public const string TokenStructList = "token_detail";
            public const string TokenTagList = "token_tag_list";
            public const string TokenTagListForLock = "lock_token_tag_name";
        }



    }
}
