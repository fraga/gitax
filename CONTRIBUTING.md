# How to contribute

Contributing to gitax is really simple after you have downloaded and installed the model.

## Process overview

* Clone this project
* Create a feature branch
* Develop your feature
* Push to your remote repository
* Create a pull request

Your code then will be submitted to code review and if accepted will be merged in the master branch.

## Commiting code

Past experiences with AX and TFS led to most of developers checking in huge amount of code against one changeset. 

With git and gitax things are a little bit different. Since we're using feature branches please do frequent and small/concise commits. Clarity on the description of the commits are also welcome as it facilitates code reviewing.

# Coding guidelines

We follow Microsoft best practices for Dynamics AX.

* Declare variables as locally as possible.
* Check the error conditions in the beginning; return/abort as early as possible.
* Have only one successful return point in the code (typically, the last statement), with the exception of switch cases, or when checking for start conditions.
* Keep the building blocks (methods) small and clear. A method should do a single, well-defined job. It should therefore be easy to name a method.
* Put braces around every block of statements, even if there is only one statement in the block.
* Put comments in your code, telling others what the code is supposed to do, and what the parameters are used for.
* Do not assign values to, or manipulate, actual parameters that are "supplied" by value. You should always be able to trust that the value of such a parameter is the one initially supplied. Treat such parameters as constants.
* Clean up your code; delete unused variables, methods and classes.
* Never let the user experience a runtime error. Take appropriate actions to either manage the situation programmatically or throw an error informing the user in the Infolog about the problem and what actions can be taken to fix the problem.
* Never make assignments to the "this" variable.
* Avoid dead code. (See Dead Code Examples.)
* Reuse code. Avoid using the same lines of code in numerous places. Consider moving them to a method instead.
* Never use infolog.add directly. Use the indirection methods: error, warning, info and checkFailed.
* Design your application to avoid deadlocks.

# Additional Resources

[Best Practices for Microsoft Dynamics AX Development AX 2012](http://msdn.microsoft.com/en-us/library/aa658028.aspx)
[Top Best Practices to Consider AX 2012](http://msdn.microsoft.com/EN-US/library/cc967435.aspx)

