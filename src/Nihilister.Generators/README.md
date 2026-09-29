## Generators

Project which contains source generators

---
### 1) Localized Strings Generator

    -- Why --
    Type safe response strings access, and enforces correct usage of response strings.

    -- How it works --
    Creates a file "strs.cs" containing a class called "strs" in "NadekoBot" namespace.
    
    Loads "Modules/**/strings/res.yml" and creates a property or a function for each key in the res.yml file based on whether the value has string format placeholders or not.

    - If a value has no placeholders, it creates a property in the strs class which returns an instance of a LocStr struct containing only the key and no replacement parameters
    
    - If a value has placeholders, it creates a function with the same number of arguments as the number of placeholders, and passes those arguments to the LocStr instance

    -- How to use --
    1. Add a new key to strings/res.yml in your module/command folder - "greet_me": "Hello, {0}"
    2. You now have access to a function strs.greet_me(obj p1)
    3. Using "GetText(strs.greet_me("Me"))" will return "Hello, Me"

---

### 2) Clonable

    Generates clonable attribute and code to clone classes
    