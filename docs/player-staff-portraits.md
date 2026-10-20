# Player and staff presentation

Player profiles, roster tables, statistics links, recruit cards and staff profiles now use offline illustrated headshots. The illustrations are fictional; they do not represent imported players' real appearances.

For procedural portraits, appearance is derived from the existing player/prospect or staff GUID using explicit unsigned arithmetic. No new save fields, random-number consumption, downloaded assets or changes to recruiting/simulation logic are required. Team changes update uniform colors without changing the face. Staff use polo collars and age-dependent gray hair/glasses. Archive links retain their identity; archived player attributes remain unavailable as before.

Profile headers emphasize name, role/class and school. Character, physical and skill ratings use compact color-coded tiles that reflow from one to four columns with available width. Tables retain name sorting and accessible profile buttons. Staff-management rows are grouped into cards; recruit portraits use neutral colors before enrollment.

Regression tests cover identity after serialization and transfers, appearance diversity and palette bounds, and a fixed identity snapshot to prevent unintentional face changes. This is a vector portrait foundation; photo imports and portrait customization are not yet included.

Imported players can now carry NCAA `PSKI`, `PHCL`, `PHED` and `PFMP` appearance codes. These take priority over identity-derived defaults, including in the bundled NEXT26 roster. Hair-color mappings include blonde/red/gray and the renderer distinguishes cornrows, flat top, fade, balding, mullet and dreadlocks. Face codes provide deterministic illustrated shape/details; the original game textures are not bundled. Height and weight accompany player and recruit profiles and sortable roster columns.
