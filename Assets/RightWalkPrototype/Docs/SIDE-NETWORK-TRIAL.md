# Side-scroll network trial

Scene: `Assets/RightWalkPrototype/SideScrollNetworkTrial.unity`.
The original local recovery scene is unchanged.

Run the scene as Local Host and another player build as Local Client.
Both use loopback port 7783. This is same-PC testing, not Relay matchmaking.
Player build: `Builds/SideScrollNetwork/SideScrollNetwork.exe`.
Command-line switches: `--side-host`, `--side-client`, `--side-test`.
The automated test writes `Temp/side-network-host.txt` and
`Temp/side-network-client.txt`, then exits.

Only the owning player reads movement, aim and firing input. Position,
gait frame, grounded state and aim are owner-written snapshots; remote
players render those snapshots. This is a prototype, not server-authoritative
movement or anti-cheat validation.

The owner requests a shot using the actual muzzle origin and barrel direction.
The server checks finite inputs, direction magnitude, origin distance and
cooldown, and selects the first non-trigger obstruction. A client RPC sends
the approved origin, direction, travel distance and impact flag to everyone.
Each client renders the same trajectory and visual effects. These bullets
are not NetworkObjects; the server now applies damage to trial targets. There is no
owner prediction, latency compensation or late-join replay of active bullets.
RPC receive counts verify transport delivery, not screenshot equality.

Verified on 2026-10-12: Windows development build succeeded with zero errors.
Two separate batch-mode player processes connected over loopback; both reports
passed with two players, three remote shot events and three local requests.
No exception/error matches were found in either test process log.

The existing top-down `NetworkPlayerController` / `NetworkProjectile` pipeline
is unchanged. Integrating the side-scroll rig into that production pipeline
remains separate from this two-client visual test.

## Damage targets

The trial now spawns two registered network target prefabs at x=-3 and x=3.
Each starts with 3 HP. Accepted shots trace against the first obstruction on
the server; a target receives 1 damage after distance / bullet speed seconds.
Clients do not issue damage and only display replicated HP/hit counters.
At zero HP the target tilts, dims and disables its collider on every peer.
The host's Reset Targets button restores health and advances a generation
counter, invalidating pending impacts from before the reset.

Run each process with `--side-damage-test` instead of `--side-test` to check
that both peers see both targets at HP=0, exactly 3 hits and disabled collision.
The same host/client report paths are used. These targets are not player PvP
health or the production combat integration.

Damage validation (2026-10-12): two separate Windows players passed with
both targets HP=0, hits=3 and collider disabled on both peers. Both processes
exited 0 and their logs contained no error/exception matches. Editor host
checks also verified reset to 3 HP, no extra hits on dead targets, and rejection
of impacts from a previous reset generation. Target art was screenshot-checked.

## Air down-shot pose

Deep airborne aim blends from muzzle-to-pointer solving to direction-based
aim over 55..85 degrees downward. At 90 degrees the barrel is exactly vertical,
even though the gun is translated ahead of the torso. Ground remains capped
at 55 degrees. The support forearm remains in front for deep airborne aim.
AirDownShotPose reuses accepted run frame 5 as a temporary tucked-leg stance;
it does not redraw or replace the approved character art. On landing it
restores the locomotion frame and clears its hip height override.

Network snapshots now carry actual barrel direction instead of a constructed
world target point, avoiding angle drift caused by shifted gun origins.
`--side-air-test` validates remote vertical direction, tucked sprite and shot
delivery in two processes. Full jump animation and polished pose transitions
remain follow-up work. A 72-direction arm sweep (both sides, 55..90 degrees)
found no unreachable grips after increasing gun height slightly at 75..90.
Two Windows processes passed the air test on 2026-10-12: both verified the
remote vertical barrel, tucked-leg sprite and three remote shot events.
Both exited 0 with no exception/error matches in their logs. A landing check
confirmed the -55-degree ground limit and cleared hip override.
