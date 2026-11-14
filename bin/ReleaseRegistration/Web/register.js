(function () {
    var form = document.getElementById("registration-form");
    var message = document.getElementById("form-message");
    var submitButton = form.querySelector("button[type=submit]");

    form.addEventListener("submit", function (event) {
        event.preventDefault();
        message.textContent = "";
        message.className = "message";

        var password = form.elements.password.value;
        if (password !== form.elements.passwordConfirm.value) {
            message.textContent = "The passwords do not match.";
            message.className = "message error";
            return;
        }

        var body = new URLSearchParams(new FormData(form));
        submitButton.disabled = true;

        fetch("/api/register", {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8" },
            body: body.toString()
        }).then(function (response) {
            return response.json().then(function (result) {
                return { ok: response.ok, result: result };
            });
        }).then(function (response) {
            message.textContent = response.result.message;
            if (!response.ok)
                message.className = "message error";
            else
                form.reset();
        }).catch(function () {
            message.textContent = "Could not reach the registration service. Please try again later.";
            message.className = "message error";
        }).then(function () {
            submitButton.disabled = false;
        });
    });
}());
