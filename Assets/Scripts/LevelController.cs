using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelController : MonoBehaviour
{
    public static int[] HeldIndexToNote = new int[]{
        //        U  D  UD    
        /*  */ 0, 3, 7, 0,
        /* R*/ 5, 4, 6, 5,
        /* L*/ 1, 2, 8, 1,
        /*RL*/ 0, 3, 7, 0
    };

    float thresholdGreat = 0.03f;
    float thresholdGood = 0.06f;
    float thresholdOk = 0.12f;

    public float scale = 1.0f;
    public float health = 1.0f;

    public float latencyCorrection = 0.0f;
    

    public ParticleSystem particles;
    public AudioClip[] clips;
    public TextAsset levelData;
    public Dictionary<string, MusicSection> sections = new Dictionary<string, MusicSection>();
    public MusicSection currSection;
    public MusicSection nextSection;
    public AudioSource musicSource;
    public AudioSource musicSource2;
    public AudioSource oneshots;
    public GameObject lineSegmentPrefab;
    List<LineSegment> baseSegments = new List<LineSegment>();
    public int sectionCurrPieceIndex = -1;
    int lastHeldNote = 0;
    bool lastPlayHeld = false;
    public SpriteRenderer centerVis;
    public Sprite[] centerVisSprites;
    public Vector3 shakeDir = new Vector3();
    public CharacterThingy charthing;
    public GameObject particleThingy;
    public AudioClip youwin;
    public AudioClip youlose;
    public FadeOut fader;
    public Camera camerathing;
    void Awake()
    {
        int bufferLength;
        int numBuffers;
        AudioSettings.GetDSPBufferSize(out bufferLength, out numBuffers);
        latencyCorrection = (float)bufferLength / (float)AudioSettings.outputSampleRate;
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.volume = 0.3f;
        musicSource2 = gameObject.AddComponent<AudioSource>();
        musicSource2.volume = 0.3f;
        oneshots = gameObject.AddComponent<AudioSource>();
        ReadLevelData(levelData.text);
    }
    // Start is called before the first frame update
    void Start()
    {
        MusicSection startSect;
        sections.TryGetValue("start", out startSect);
        StartSection(startSect, AudioSettings.dspTime);
    }

    float MusicTime() {
        return musicSource.time - latencyCorrection;
    }

    // Update is called once per frame
    void Update()
    {
        camerathing.backgroundColor = Color.Lerp(new Color(0, 0.5f, 1), new Color(1, 0, 0), 1-health);
        bool leftHeld = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool rightHeld = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        bool upHeld = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool downHeld = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        int heldIndex = (upHeld ? 1 : 0) + (downHeld ? 2 : 0) + (rightHeld ? 4 : 0) + (leftHeld ? 8 : 0);
        int heldNote = HeldIndexToNote[heldIndex];
        bool playHeld = Input.GetKey(KeyCode.Space);

        if(playHeld && !lastPlayHeld && currSection != null) currSection.lastAttackTime = MusicTime();
        if(!playHeld && lastPlayHeld && currSection != null) currSection.lastReleaseTime = MusicTime();
        if(heldNote != lastHeldNote) currSection.lastNoteChangeTime = MusicTime();

        if(heldNote != 0)
            charthing.targetAngle = NoteToAngle(heldNote) * 180 / Mathf.PI;

        lastPlayHeld = playHeld;
        lastHeldNote = heldNote;
        centerVis.sprite = centerVisSprites[heldNote];
        centerVis.transform.localScale = playHeld ? new Vector3(1.3f,1.3f,1.3f) : new Vector3(1.0f,1.0f,1.0f);
        bool doParticles = false;
        
        for(int i = 0; i < baseSegments.Count; i++) {
            LineSegment seg = baseSegments[i];
            float musicTime = (seg.piece.section == currSection) ? MusicTime() : (MusicTime() - currSection.transitionTime);
            if(seg.piece.isNote && musicTime >= seg.piece.startTime && musicTime <= seg.piece.endTime && seg.piece.section == currSection) {
                // Handle attack/release/whatever
                if(seg.attackDone) {
                    if(!seg.releaseDone) {
                        doParticles = true;
                        if(heldNote != seg.piece.note || !playHeld) {
                            seg.releaseDone = true;
                            float proportionAchieved = Mathf.InverseLerp(seg.piece.startTime, seg.piece.endTime, musicTime);
                            Debug.Log("Release at " + proportionAchieved);
                        }
                    }
                } else {
                    float attackTimeDiff = Mathf.Abs((seg.piece.isAttack ? currSection.lastAttackTime : currSection.lastNoteChangeTime) - seg.piece.startTime);
                    if(attackTimeDiff < thresholdOk && ((seg.piece.isAttack) == (currSection.lastNoteChangeTime <= currSection.lastAttackTime)) && heldNote == seg.piece.note) {
                        seg.attackDone = true;
                        if(attackTimeDiff < thresholdGreat) Debug.Log("GREAT!");
                        else if(attackTimeDiff < thresholdGood) Debug.Log("GOOD!");
                        else Debug.Log("OKAY!");
                    } else if(musicTime >= seg.piece.startTime + thresholdOk) {
                        HandleMiss(seg);
                        seg.attackDone = true;
                    }
                }
            }
            while(seg != null && musicTime > seg.piece.endTime) {
                LineSegment next = seg.next;
                if(next) {
                    next.transform.SetParent(transform);
                    baseSegments[i] = next;
                }
                seg.next = null;
                RecycleSegment(seg);
                seg = next;
                if(!seg) {
                    baseSegments.RemoveAt(i);
                    i--;
                    break;
                }
            }
            if(!seg) continue;
            UpdateApproachingSegment(seg);
        }
        if(currSection != null) {
            if(AudioSettings.dspTime - currSection.startTime >= currSection.transitionTime / musicSource.pitch) {
                if(nextSection != null) {
                    // transition the attack/release/whatever times over
                    nextSection.lastAttackTime = currSection.lastAttackTime - currSection.transitionTime;
                    nextSection.lastReleaseTime = currSection.lastReleaseTime - currSection.transitionTime;
                    nextSection.lastNoteChangeTime = currSection.lastNoteChangeTime - currSection.transitionTime;

                    StartSection(nextSection, currSection.startTime + currSection.transitionTime);
                } else {
                    fader.target = 1;
                    fader.fadeTask = "Scenes/Menu";
                    oneshots.PlayOneShot(youwin);
                    enabled = false;
                }
            }
            UpdateSection(currSection, MusicTime());
            if(nextSection != null) {
                UpdateSection(nextSection, MusicTime() - currSection.transitionTime);
            }
            if(currSection.nextEvent < currSection.events.Count) {
                MusicEvent evt = currSection.events[currSection.nextEvent];
                while(evt != null && evt.time >= MusicTime()) {
                    evt.DoEvent(this);
                    currSection.nextEvent++;
                    if(currSection.nextEvent < currSection.events.Count) {
                        evt = currSection.events[currSection.nextEvent];
                    } else {
                        evt = null;
                    }
                }
            }
            if(sectionCurrPieceIndex + 1 < currSection.pieces.Count) {
                MusicPiece nextPiece = currSection.pieces[sectionCurrPieceIndex + 1];
                if(MusicTime() >= nextPiece.startTime) {
                    sectionCurrPieceIndex++;
                }
            }

        }

        if(shakeDir.sqrMagnitude != 0) {
            float mag = shakeDir.magnitude;
            Vector3 invNormalized = -shakeDir.normalized;
            mag = Mathf.Min(mag, Time.deltaTime * 0.4f);
            shakeDir = shakeDir + invNormalized * mag;
            float shakeMult = Mathf.Cos(Time.time * 40);
            Vector3 localPos = transform.localPosition;
            localPos.x = shakeMult * shakeDir.x;
            localPos.y = shakeMult * shakeDir.y;
            transform.localPosition = localPos;
        }
        if(doParticles && !particles.isEmitting)
            particles.Play();
        else if(!doParticles && particles.isEmitting)
            particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    void HandleMiss(LineSegment seg) {
        float angle = NoteToAngle(seg.piece.noteArcFrom);
        shakeDir += new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.1f;
        health -= 0.05f;
        Debug.Log("MISS!");
        if(health <= 0) {
            fader.target = 1;
            musicSource.Stop();
            musicSource2.Stop();
            enabled = false;
            oneshots.PlayOneShot(youlose);
            fader.fadeTask = SceneManager.GetActiveScene().path;
        }
    }

    void UpdateSection(MusicSection section, float time) {
        if(section.nextUnloadedPiece < section.pieces.Count) {
            MusicPiece piece = section.pieces[section.nextUnloadedPiece];
            if((piece.startTime - time) < 10.0f) {
                section.nextUnloadedPiece++;
                LineSegment seg = NewSegment();
                seg.piece = piece;
                seg.MakeArc(NoteToAngle(piece.noteArcFrom), NoteToAngle(piece.noteArcTo), (piece.endTime - piece.startTime) * scale);
                seg.UpdateMaterial();
                if(section.currTail && piece.connected) {
                    seg.transform.SetParent(section.currTail.transform);
                    seg.transform.localPosition = section.currTail.end;
                    section.currTail.next = seg;
                } else {
                    seg.transform.SetParent(transform);
                    UpdateApproachingSegment(seg);
                    baseSegments.Add(seg);
                }
                section.currTail = seg;
            }
        }
    }

    void UpdateApproachingSegment(LineSegment seg) {
        MusicPiece piece = seg.piece;
        float musicTime = (piece.section == currSection) ? MusicTime() : (MusicTime() - currSection.transitionTime);
        float fromAngle = NoteToAngle(piece.noteArcFrom);
        if(musicTime > piece.startTime) {
            float toAngle = NoteToAngle(piece.noteArcTo);
            seg.transform.localPosition = Vector3.zero;
            float fac = (musicTime - piece.startTime) / (piece.endTime - piece.startTime);
            seg.MakeArc(fromAngle, toAngle, (piece.endTime - piece.startTime) * scale, fac);
            if(seg.next) {
                seg.next.transform.localPosition = seg.end;
            }
        } else {
            float approachDistance = (piece.startTime - musicTime) * scale;
            seg.transform.localPosition = new Vector3(Mathf.Cos(fromAngle) * approachDistance, Mathf.Sin(fromAngle) * approachDistance);
        }
    }

    LineSegment NewSegment() {
        return GameObject.Instantiate(lineSegmentPrefab).GetComponent<LineSegment>();
    }

    void RecycleSegment(LineSegment seg) {
        if(seg.next) RecycleSegment(seg.next);
        seg.next = null;
        seg.attackDone = false;
        seg.releaseDone = false;
        GameObject.Destroy(seg.gameObject);
    }

    void StartSection(MusicSection section, double time) {
        sectionCurrPieceIndex = 0;
        section.startTime = AudioSettings.dspTime;
        currSection = section;
        nextSection = null;
        AudioSource newMusicSource = musicSource2;
        musicSource2 = musicSource;
        musicSource = newMusicSource;
        newMusicSource.Stop();
        newMusicSource.clip = clips[section.clipId];
        newMusicSource.PlayScheduled(time);

        for(int i = 0; i < baseSegments.Count; i++) {
            LineSegment seg = baseSegments[i];
            if(seg.piece.section == null || (seg.piece.section != currSection && seg.piece.section != nextSection)) {
                RecycleSegment(seg);
                baseSegments.RemoveAt(i);
                i--;
                continue;
            }
        }
    }

    void ReadLevelData(string str) {
        string[] lines = str.Split('\n');
        MusicSection currMusicSection = null;
        bool runsepFlag = false;
        float timeBase = 0;
        float timeScale = 1;
        foreach(string line in lines) {
            try {
                int firstSpaceIndex = line.IndexOf(' ');
                string key = firstSpaceIndex == -1 ? line : line.Substring(0, firstSpaceIndex);
                string value = firstSpaceIndex == -1 ? "" : line.Substring(firstSpaceIndex+1);
                key = key.Trim(); value = value.Trim();
                if(key == "section") {
                    currMusicSection = new MusicSection();
                    currMusicSection.name = value;
                    timeBase = 0;
                    timeScale = 1;
                    sections.Add(value, currMusicSection);
                } else if(key == "rebase") {
                    string[] parts = value.Trim().Split(' ');
                    float newBase = parts.Length >= 1 ? float.Parse(parts[0]) : 0;
                    float newScale = parts.Length >= 2 ? (60 / float.Parse(parts[1])) : timeScale;
                    timeBase = timeBase + (newBase * timeScale);
                    timeScale = newScale;
                } else if(key == "transition") {
                    currMusicSection.transitionTime = float.Parse(value) * timeScale + timeBase;
                } else if(key == "setnext") {
                    MusicEventSetNext evt = new MusicEventSetNext();
                    string[] parts = value.Split(' ');
                    evt.time = float.Parse(parts[0]) * timeScale + timeBase;
                    evt.possibilities = parts[1].Split(',');
                    currMusicSection.events.Add(evt);
                } else if(key == "play") {
                    PlayEvent evt = new PlayEvent();
                    string[] parts = value.Split(' ');
                    evt.time = float.Parse(parts[0]) * timeScale + timeBase;
                    evt.index = int.Parse(parts[1]);
                    currMusicSection.events.Add(evt);
                } else if(key == "clip") {
                    currMusicSection.clipId = int.Parse(value);
                } else if(key == "run") {
                    string[] parts = value.Split(',');
                    int[] notes = new int[parts.Length-1];
                    float[] noteTimes = new float[parts.Length];
                    int earliestNonzeroNote = 0;
                    for(int i = 0; i < parts.Length-1; i++) {
                        string[] partparts = parts[i].Split(':');
                        notes[i] = int.Parse(partparts[0].Trim());
                        noteTimes[i] = timeBase + timeScale * float.Parse(partparts[1].Trim());
                        if(earliestNonzeroNote == 0 && notes[i] != 0) earliestNonzeroNote = notes[i];
                    }
                    noteTimes[parts.Length-1] = timeBase + timeScale * float.Parse(parts[parts.Length-1].Trim());

                    MusicPiece lastPiece = currMusicSection.pieces.Count > 0 ? currMusicSection.pieces[currMusicSection.pieces.Count-1] : null;
                    MusicPiece joiner = null;
                    if(lastPiece != null && !runsepFlag) {
                        if(lastPiece.endTime >= noteTimes[0]) {
                            joiner = lastPiece;
                        } else {
                            joiner = new MusicPiece();
                            joiner.section = currMusicSection;
                            joiner.note = 0;
                            joiner.startTime = lastPiece.endTime;
                            joiner.endTime = noteTimes[0];
                            joiner.noteArcFrom = lastPiece.noteArcTo;
                            joiner.noteArcTo = earliestNonzeroNote == 0 ? joiner.noteArcFrom : earliestNonzeroNote;
                            joiner.isAttack = false;
                            currMusicSection.pieces.Add(joiner);
                        }
                    }
                    for(int i = 0; i < notes.Length; i++) {
                        MusicPiece piece = new MusicPiece();
                        piece.section = currMusicSection;
                        piece.isAttack = (i == 0);
                        piece.isNote = true;
                        piece.connected = joiner != null;
                        piece.startTime = noteTimes[i];
                        piece.endTime = noteTimes[i+1];
                        piece.note = notes[i];
                        if(piece.note != 0) {
                            piece.noteArcFrom = piece.note;
                            piece.noteArcTo = piece.note;
                        } else {
                            if(joiner != null) piece.noteArcFrom = joiner.noteArcTo;
                            else if(earliestNonzeroNote != 0) piece.noteArcFrom = earliestNonzeroNote;
                            else if(lastPiece != null) piece.noteArcFrom = lastPiece.noteArcTo;
                            else piece.noteArcFrom = 1;
                            if(i < notes.Length-1 && notes[i+1] > 0) piece.noteArcTo = notes[i+1];
                            else piece.noteArcTo = piece.noteArcFrom;
                        }
                        currMusicSection.pieces.Add(piece);
                        joiner = piece;
                    }
                    runsepFlag = false;
                } else if(key == "runsep") {
                    runsepFlag = true;
                }
            } catch(System.Exception e) {
                Debug.Log(line);
                throw e;
            }
        }
    }
    float NoteToAngle(int note) {
        return Mathf.PI - ((note-1) * Mathf.PI / 4);
    }
}

public class MusicSection {
    public int clipId;
    public string name;
    public List<MusicPiece> pieces = new List<MusicPiece>();
    public int nextUnloadedPiece = 0;
    public List<MusicEvent> events = new List<MusicEvent>();
    public int nextEvent = 0;
    public float transitionTime = 10;
    public LineSegment currTail;
    public double startTime = 0;

    public float lastAttackTime = -100;
    public float lastReleaseTime = -100;
    public float lastNoteChangeTime = -100;
    public void Reset() {
        nextUnloadedPiece = 0;
        nextEvent = 0;
        lastAttackTime = -100;
        lastReleaseTime = -100;
        lastNoteChangeTime = -100;
        currTail = null;
    }
}

public abstract class MusicEvent {
    public float time;

    public abstract void DoEvent(LevelController cont);
}

public class MusicEventSetNext : MusicEvent {
    public string[] possibilities;
    public override void DoEvent(LevelController cont) {
        if(cont.nextSection == null) {
            cont.sections.TryGetValue(possibilities[Random.Range(0, possibilities.Length)], out cont.nextSection);
            if(cont.nextSection != null) {
                MusicSection section = cont.nextSection;
                section.Reset();
            }
        }
    }
}
public class PlayEvent : MusicEvent {
    public int index;
	public override void DoEvent(LevelController cont)
	{
		cont.oneshots.PlayOneShot(cont.clips[index]);
	}
}

public class MusicPiece {
    public MusicSection section;
    public float startTime;
    public float endTime;
    public int note = 0;
    public int noteArcFrom = 0;
    public int noteArcTo = 0;
    public bool isAttack = true;
    public bool isNote = false;
    public bool connected = true;
}
