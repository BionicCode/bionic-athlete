# Parsed Garmin Connect PDF Reference - 2026-03-23 Activity

Source references:
- `2026-03-23_Garmin_Report.pdf`
- `2026-03-23_Garmin_Report_page 2.pdf`
- `2026-03-23_Garmin_Report_page 3.pdf`
- prior `garmin_connect_pdf_parsed_reference.md`

Purpose:
This file is a normalized human-readable oracle for Codex when validating View C output. It should be used as the minimum visible-field reference for the human-readable report. Values that are not present in the FIT/View A/View B source may be rendered as empty/unavailable, but the user-facing field/section should usually still exist when Garmin Connect shows it.

Important scope note:
Garmin Connect may combine FIT-file data with Garmin account/server metadata. The View C report must not fabricate server-only values as direct FIT values. Instead, mark them as unavailable or account/server-only when they cannot be found or derived from the local decoded data set.

## Page 1 - Activity header and first charts

Header / activity identity:
- Activity kind/title: Indoor Cycling
- Date/time shown by Garmin Connect: 23 March 2026 @ 16:25
- Event Type: Uncategorized
- Course: --
- Gear indicator: 1

Top summary metrics:
- Distance: 6.18 km
- Time: 20:01
- Avg Speed: 18.5 kph
- Total Ascent: --
- Avg Power: 74 W

Visible charts:
- Speed over time
- Heart Rate over time
- Bike Cadence over time
- Power over time on the main chart sequence

Chart axis expectation:
- Time axis labels are shown at regular elapsed-time positions such as 1:40, 3:20, 5:00, 6:40, 8:20, 10:00, 11:40, 13:20, etc.
- Charts are horizontal and occupy the content width.

## Page 2 - Additional charts and Power Curve tab entry

Visible tabs:
- Activity Stats
- Laps
- Time in Zones
- Power Curve

Visible charts/series:
- Bike Cadence
- Respiration Rate
- Temperature
- Stamina
- Stamina Potential

Stamina chart callout example:
- Stamina: 96%
- Potential: 96%

Power Curve tab text:
- Compare this activity's power data to your power curve.
- Time ranges shown: 4 Weeks, 3 Months, 12 Months, All, View Full Report

Power curve note for implementation:
The historical 4 Weeks / 3 Months / 12 Months / All curves are probably Garmin account/history-derived, not single-activity FIT-file data. For the local application, these should later be generated from the local persisted activity database and should include a note that they reflect the local data set, not Garmin server history. In View C v1 without persistence, do not fabricate these historical curves.

Notes area:
- Notes section present
- Prompt: How was your ride?
- Counter: 0/2000
- Photos section present

## Page 3 - Time in Zones tab

Heart Rate Zones:
- Zone 5 > 173 bpm - Maximum: 0:00, 0%
- Zone 4 154 - 172 bpm - Threshold: 0:00, 0%
- Zone 3 134 - 153 bpm - Aerobic: 7:51, 39%
- Zone 2 115 - 133 bpm - Easy: 9:16, 46%
- Zone 1 96 - 114 bpm - Warm Up: 2:42, 13%

Power Zones:
- Zone 7 > 283 Watts - Neuromuscular: 0:00, 0%
- Zone 6 248 - 282 Watts - Anaerobic: 0:00, 0%
- Zone 5 222 - 247 Watts - VO2 Max: 0:00, 0%
- Zone 4 205 - 221 Watts - Threshold: 0:00, 0%
- Zone 3 177 - 204 Watts - Tempo: 0:00, 0%
- Zone 2 130 - 176 Watts - Endurance: 0:00, 0%
- Zone 1 0 - 129 Watts - Active Recovery: 20:00, 99%

Graph expectation for View C:
- Render these as horizontal zone bar charts when the data is available.
- Each row should show zone name, range, label, duration, and percentage.
- If explicit FIT time-in-zone data is absent but zones can be reliably derived from record stream + zone definitions, the report may derive and mark the values as derived.
- If zone definitions or thresholds are missing, render the section with unavailable/insufficient-source notes rather than fabricating Garmin Connect values.

## Page 4 / Page 5 - Activity stats, device, and gear

Nutrition & Hydration:
- Resting Calories: 36
- Active Calories: 89
- Total Calories Burned: 125
- Calories Consumed: --
- Calories Net: -125
- Est. Sweat Loss: 77 ml
- Fluid Consumed: -- ml
- Fluid Net: -77 ml

Respiration Rate:
- Avg Respiration Rate: 29 brpm
- Min Respiration Rate: 16 brpm
- Max Respiration Rate: 41 brpm

Self Evaluation:
- How did you feel?: Strong
- Perceived Effort: 2/10 Light

Stamina:
- Beginning Potential: 100%
- Ending Potential: 92%
- Min Stamina: 92%

Training Effect:
- Primary Benefit: Recovery (Low Aerobic)
- Aerobic: 1.6 Some Benefit
- Anaerobic: 0.0 No Benefit
- Exercise Load: 10

Heart Rate:
- Avg HR: 127 bpm
- Max HR: 140 bpm

Timing:
- Time: 20:01
- Moving Time: 19:59
- Elapsed Time: 20:55

Power:
- Avg Power: 74 W
- Max Power: 117 W
- Max Avg Power (20 min): 72 W
- Normalized Power (NP): 75 W
- Intensity Factor (IF): 0.318
- Training Stress Score (TSS): 3.3
- FTP Setting: 236 W
- Work: 89 kJ

Pace/Speed:
- Avg Speed: 18.5 kph
- Avg Moving Speed: 18.6 kph
- Max Speed: 25.5 kph

Bike Cadence:
- Avg Bike Cadence: 80 rpm
- Max Bike Cadence: 121 rpm

Strokes:
- Total Strokes: 1,614

Temperature:
- Avg Temp: 19.0 °C
- Min Temp: 18.0 °C
- Max Temp: 20.0 °C

Intensity Minutes:
- Moderate: 15 min
- Vigorous: 0 min x2
- Total: 15 min

Device:
- Device: Edge 840
- Software: 30.18
- Summary Data: Original

Gear:
- Gear name: Strada 800
- Gear model/details: Stevens Strada 800
- Activities: 349 activities
- First use: 16 Apr 2021
- Distance usage: 5,182.8 of 3,000.0 kilometers
- Status: Exceeded max use

Legal/trademark footnote shown by Garmin:
- Normalized Power (NP), Intensity Factor (IF), and Training Stress Score (TSS) are registered trademarks of Peaksware, LLC.

## Normalized minimum View C field list

Activity identity:
- Activity kind/title
- Ride date and local start time
- Local end time when available or derivable
- Event Type
- Course
- Gear summary

Top summary:
- Distance
- Time
- Avg Speed
- Avg Power
- Total Ascent, if available; otherwise display --

Nutrition & Hydration:
- Resting Calories
- Active Calories
- Total Calories Burned
- Calories Consumed
- Calories Net
- Est. Sweat Loss
- Fluid Consumed
- Fluid Net

Respiration:
- Avg Respiration Rate
- Min Respiration Rate
- Max Respiration Rate

Self Evaluation:
- How did you feel?
- Perceived Effort

Stamina:
- Beginning Potential
- Ending Potential
- Min Stamina
- Stamina/Potential chart if time-series data exists; otherwise summary values only

Training Effect:
- Primary Benefit
- Aerobic Training Effect
- Anaerobic Training Effect
- Exercise Load

Heart Rate:
- Avg HR
- Max HR
- Heart Rate chart
- Heart Rate Zones chart/table when available or derivable

Timing:
- Time / Timer Time
- Moving Time
- Elapsed Time
- Local Start Time
- Local End Time when available or derivable

Power:
- Avg Power
- Max Power
- Max Avg Power (20 min)
- Normalized Power (NP)
- Intensity Factor (IF)
- Training Stress Score (TSS)
- FTP Setting
- Work
- Power chart
- Power Zones chart/table when available or derivable
- Power Curve is deferred until local persistence/history exists unless single-activity curve is explicitly requested and derivable

Pace/Speed:
- Avg Speed
- Avg Moving Speed
- Max Speed
- Speed chart

Bike Cadence:
- Avg Bike Cadence
- Max Bike Cadence
- Cadence chart

Strokes:
- Total Strokes

Temperature:
- Avg Temp
- Min Temp
- Max Temp
- Temperature chart

Notes / Photos:
- Notes section may be shown only if available from local app/Garmin source. If not stored locally, it may be omitted or shown as unavailable, but do not fabricate.
- Photos are out of scope unless the local application stores/imports them.

Device / source metadata:
- Device name
- Software version
- Summary Data

Gear metadata:
- Gear name
- Gear model/details
- Activity count
- First use
- Distance usage
- Status such as exceeded max use
