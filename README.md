# [Store module] Math
Math module for Store. Answer correctly and get rewarded. Automatically generates math problems with configurable questions, rewards, and difficulty

# Config
Config will be auto generated. Default:
```json 
{
    "question_interval_seconds": 120, // Interval in seconds between math questions
    "answer_timeout_seconds": 90, // // Time allowed for players to answer (in seconds)
    "max_credits": 100, /// Maximum credits awarded for a correct answer
    "min_credits": 10, // Minimum credits awarded for a correct answer
    "max_number": 100, // Maximum number used in math questions
    "min_number": 1, // Minimum number used in math questions
    "operator_chances": { 
        "+": 40, // 40% chance for the operator to be "+"
        "-": 20, // 20% chance for the operator to be "-"
        "*": 25,
        "/": 15
    },
    "operator_quantity_chances": {
        "1": 10, // 10% chance to have only 1 operator in the math expression
        "2": 20, // 20% chance to have 2 operators in the math expression
        "3": 40,
        "4": 15 // 15% chance to have 4 operators in the math expression, can add more
    },
    "reward_type": 2 // 1 = Difficulty-based rewards | 2 = Random rewards
}
```
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/L4L611665R)
