# azul-bot
A C# client + lambda that works with azul-vibe. Runs an MCTS based reinforced learning model that plays Azul.

## AzulLamba
AzulLambda contains the lambda function.
It takes a game state as input and uses MCTS to determine the next best action and returns it.
As a lambda, it is memoryless and therefore starts over every time it is invoked.
Yes this is suboptimal, but that's just part of the fun of using MCTS in a serverless and standalone function.

## LambdaProxy
This is super barebones backend service that exposed the lambda for invokation. Calling the route '/lambda' will invoke the lambda.
The heartbeat exists so that the corresponding [frontend](https://github.com/chillingo117/azul-vibe) of the game knows whether the AI is available or not.

