All new localization data should starts on a new line.
Example:
"game_name|Battle City"
"controls_text|Controls"

Localization template: Code|(Sentence + control sequences). Example: "game_name|Battle city".

Control sequences allows to manipulate the text view(only sentences can have control sequences, not codes), all sequences starts with "|". Example: "Top text |n Bottom text".
Known sequences:
|n - Sets text to a new line.
|| - Pastes a vertical line character "|".