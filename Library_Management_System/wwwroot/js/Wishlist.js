document.addEventListener("DOMContentLoaded", function () {

  document.querySelectorAll(".wishlist-btn")
    .forEach(btn => {

      btn.addEventListener("click", async function () {

        const bookId =
          parseInt(this.dataset.bookid);

        if (!bookId) return;

        const body =
          new URLSearchParams();

        body.append(
          "bookId",
          bookId
        );

        try {

          const response =
            await fetch(
              '/Member/Wishlist/Toggle',
              {
                method: 'POST',
                headers: {
                  'Content-Type':
                    'application/x-www-form-urlencoded'
                },
                body: body
              });

          const result =
            await response.json();

          if (!result.success) {

            alert(
              result.message ||
              "Login required"
            );

            return;
          }

          // update all heart buttons

          document
            .querySelectorAll(".wishlist-btn")
            .forEach(b => {

              const id =
                parseInt(
                  b.dataset.bookid
                );

              const icon =
                b.querySelector("i");

              const active =
                result.wishlistIds
                  .includes(id);

              b.classList.toggle(
                "active",
                active
              );

              if (icon) {

                if (active) {

                  icon.classList.remove(
                    "fa-regular"
                  );

                  icon.classList.add(
                    "fa-solid"
                  );

                } else {

                  icon.classList.add(
                    "fa-regular"
                  );

                  icon.classList.remove(
                    "fa-solid"
                  );
                }
              }

            });

          // UPDATE NAVBAR COUNT
          const badge =
            document.getElementById(
              "wishlistCountBadge"
            );

          if (badge) {

            badge.innerText =
              result.wishlistCount;

            if (
              result.wishlistCount <= 0
            ) {
              badge.style.display =
                "none";
            }
            else {
              badge.style.display =
                "flex";
            }
          }

        }
        catch (err) {

          console.log(err);

          alert(
            "Something went wrong"
          );
        }

      });

    });

});
