//登入與註冊
function cvOpen() {

    document.querySelector('#user').value = "";
    document.querySelector('#password').value = "";
    cv.style.display = "flex";
    document.getElementById("registerSubmit").style.display = "none";
    document.getElementById("loginMsg").textContent = "";
}

document.querySelector('#nameInput').addEventListener('click', e => {
    e.preventDefault();


    const msg = document.getElementById("loginMsg");
    const registerbtn = document.getElementById("registerSubmit");
    const name = document.querySelector('#user').value;
    const pwd = document.querySelector('#password').value;
    const cv = document.getElementById("cv");

    if (pwd == "" || name == "") {
        if (msg) msg.textContent = "帳號或密碼勿為空值!";
        return
    }


    fetch('/Account/Login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `name=${encodeURIComponent(name)}&password=${encodeURIComponent(pwd)}`
    })
        .then(r => r.json())
        .then(res => {
            if (res.ok) {
                cv.style.display = "none";
                sessionStorage.setItem("UserId", res.userId);
                sessionStorage.setItem("UserName", name);
                window.UserId = sessionStorage.getItem("UserId");
                window.UserName = sessionStorage.getItem("UserName");
                if (msg) msg.textContent = "";
            } else {
                if (res.code === "wrong_password") {
                    if (msg) msg.textContent = "密碼錯誤";
                } else if (res.code === "not_found") {
                    if (msg) msg.textContent = "查無使用者，是否新增一個新的帳號?";
                    if (registerbtn) registerbtn.style.display = "block";
                }
            }
        });


        //.then(async r => {
        //    const text = await r.text();
        //    console.log('status=', r.status, 'body=', text);
    //});

});

document.querySelector('#registerSubmit').addEventListener('click', e => {
    e.preventDefault();

    const msg = document.getElementById("loginMsg");
    const name = document.querySelector('#user').value.trim();
    const pwd = document.querySelector('#password').value;
    const cv = document.getElementById("cv");

    if (pwd == "" || name =="") {
        if (msg) msg.textContent = "密碼勿為空值!";
        return
    }

    fetch('/Account/Register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `name=${encodeURIComponent(name)}&password=${encodeURIComponent(pwd)}`
    })
        .then(r => r.json())
        .then(res => {
            if (res.ok) {
                console.log('註冊成功 userId=', res.userId);
                cv.style.display = "none";
                sessionStorage.setItem("UserId", res.userId);
                sessionStorage.setItem("UserName", name);
                window.UserId = sessionStorage.getItem("UserId");
                window.UserName = sessionStorage.getItem("UserName");
            } else {
                alert(res.msg || '註冊失敗');
            }
        });
});

document.querySelector('#logout').addEventListener('click', e => {
    e.preventDefault();

    sessionStorage.setItem("UserId", null);
    sessionStorage.setItem("UserName", null);

    cvOpen()
});
