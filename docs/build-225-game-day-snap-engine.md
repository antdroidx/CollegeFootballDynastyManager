# Build 225: shared Game Day snap engine

Game Day now offers **Sim game**, **Watch game**, and **Play game**. Watch advances CPU calls automatically. Play pauses for an exact offensive call and can switch to Watch or skip to the final result. Every mode runs the same deterministic snap resolver.

The resolver uses offensive and defensive play definitions, formation personnel, depth charts, injuries, position-specific fatigue rotation, player ratings, coordinator scheme, head-coach leadership and staff game management. Skill positions and defensive linemen rotate regularly; linebackers and defensive backs rotate mostly through packages or fatigue; offensive linemen stay together unless unavailable or critically tired.

The initial viewer presents a live scoreboard, quarter, clock, down, distance, field position, last-play result and team-colored position markers for both active elevens. Its snap-event contract is designed to support route art and richer sprites without changing simulation results.

Each snap records its offensive and defensive calls, call source, personnel, quarterback, carrier, target, primary defender, yards and outcome. Completed games persist player game lines, and season/career tables expose passing, rushing, receiving, defense, kicking, punting and returns by category.
