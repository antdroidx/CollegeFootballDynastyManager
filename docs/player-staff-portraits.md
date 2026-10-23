# Player and staff presentation

Player profiles, roster tables, statistics links, recruit cards and staff profiles now use offline illustrated headshots. The illustrations are fictional; they do not represent imported players' real appearances.

For procedural portraits, appearance is derived from the existing player/prospect or staff GUID using explicit unsigned arithmetic. Portrait generation does not consume simulation randomness or require downloaded assets. Optional appearance overrides are saved with each person. Team changes update uniform colors without changing the face. Staff use polo collars and age-dependent gray hair/glasses. Archive links retain their identity; archived player attributes remain unavailable as before.

Profile headers emphasize name, role/class and school. Character, physical and skill ratings use compact color-coded tiles that reflow from one to four columns with available width. Tables retain name sorting and accessible profile buttons. Staff-management rows are grouped into cards; recruit portraits use neutral colors before enrollment.

Regression tests cover identity after serialization and transfers, appearance diversity and palette bounds, and a fixed identity snapshot to prevent unintentional face changes. This is a vector portrait foundation; photo imports are not included.

Imported players can now carry NCAA `PSKI`, `PHCL`, `PHED` and `PFMP` appearance codes. These take priority over identity-derived defaults, including in the bundled NEXT26 roster. Hair-color mappings include blonde/red/gray and the renderer distinguishes cornrows, flat top, fade, balding, mullet and dreadlocks. Face codes provide deterministic illustrated shape/details; the original game textures are not bundled. Height and weight accompany player and recruit profiles and sortable roster columns.

## Build 263 polish

Portraits have a transparent background; the team jersey or staff polo remains. Facial hair uses filled stubble, goatee and full-beard silhouettes instead of disconnected cheek strokes. Imported face codes retain procedural facial-hair variety unless an explicit selection is supplied. Staff gain subtle age lines and salt-and-pepper temples at 45, gray hair at 55 and silver hair at 65 when hair color is automatic. Explicit imported or edited hair colors are retained.

Open a player or staff profile and choose **Edit portrait** to preview facial hair, hair color and hairstyle. Save appearance autosaves the dynasty; Cancel leaves it unchanged. Skin tone and face codes are retained. Player edits also update transfer, career and season-stat records for that identity. These controls do not alter ratings or game statistics.

The optional app CSV extension `FacialHair` accepts 0=clean shaven, 1=stubble, 2=goatee or 3=beard. Blank/absent uses the stable identity default. It is separate from the original NCAA fields.

Touchdown playback now carries both the ball and scoring player four yards into the end zone. Recorded scoring yardage remains at the goal line. Formation projection permits backfield players inside their own end zone, and curl routes stay within the rendered field.
