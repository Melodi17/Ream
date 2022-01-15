import mylib

global username = 'MyUser'
global password = Main.Read()

function checkCredentials {
    calc = 1 + 1
    if username == 'MyUser' & password == 'MyPassword' & calc == 2 {
        Main.Write('Your password is correct')
    }
    else {
        Main.Write('Credentials are incorrect')
    }
}

checkCredentials